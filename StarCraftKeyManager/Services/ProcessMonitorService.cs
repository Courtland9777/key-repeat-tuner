using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Options;
using StarCraftKeyManager.Interfaces;
using StarCraftKeyManager.Interop;
using StarCraftKeyManager.Models;

namespace StarCraftKeyManager.Services;

internal sealed class ProcessMonitorService : BackgroundService
{
    private readonly ILogger<ProcessMonitorService> _logger;
    private readonly IProcessEventWatcher _processEventWatcher;
    private readonly ConcurrentDictionary<int, byte> _trackedProcesses = new();

    private bool _isRunning;
    private KeyRepeatSettings _keyRepeatSettings;
    private string _processName;

    public ProcessMonitorService(
        ILogger<ProcessMonitorService> logger,
        IOptionsMonitor<AppSettings> optionsMonitor,
        IProcessEventWatcher processEventWatcher)
    {
        _logger = logger;
        _processEventWatcher = processEventWatcher;

        var settings = optionsMonitor.CurrentValue;
        _processName = settings.ProcessMonitor.ProcessName;
        _keyRepeatSettings = settings.KeyRepeat;

        _processEventWatcher.Configure(_processName);
        _processEventWatcher.ProcessEventOccurred += OnProcessEventOccurred;

        optionsMonitor.OnChange(updatedSettings =>
        {
            _logger.LogInformation("Configuration updated: {@Settings}", updatedSettings);
            _processName = updatedSettings.ProcessMonitor.ProcessName;
            _keyRepeatSettings = updatedSettings.KeyRepeat;
            _processEventWatcher.Configure(_processName);
            ApplyKeyRepeatSettings();
        });
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting process monitor service.");
        _processEventWatcher.Start();

        var sanitizedProcessName = _processName.Replace(".exe", "", StringComparison.OrdinalIgnoreCase);
        var initialProcesses = Process.GetProcessesByName(sanitizedProcessName);

        foreach (var process in initialProcesses)
            _trackedProcesses.TryAdd(process.Id, 0);

        _isRunning = !_trackedProcesses.IsEmpty;
        _logger.LogInformation("Initial tracked processes: {Count}", _trackedProcesses.Count);
        ApplyKeyRepeatSettings();

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (TaskCanceledException)
        {
            _logger.LogInformation("Process monitor service cancellation requested.");
        }
        finally
        {
            _logger.LogInformation("Stopping process monitor service.");
            _processEventWatcher.Stop();
            _processEventWatcher.ProcessEventOccurred -= OnProcessEventOccurred;
        }
    }

    private void OnProcessEventOccurred(object? sender, ProcessEventArgs e)
    {
        var wasRunning = _isRunning;

        switch (e.EventId)
        {
            case 4688:
                _trackedProcesses.TryAdd(e.ProcessId, 0);
                break;
            case 4689:
                _trackedProcesses.TryRemove(e.ProcessId, out _);
                break;
        }

        _isRunning = !_trackedProcesses.IsEmpty;

        _logger.LogInformation("Process event occurred: {EventId} for PID {ProcessId}", e.EventId, e.ProcessId);

        if (wasRunning == _isRunning) return;
        _logger.LogInformation("Process running state changed to {IsRunning}. Updating key repeat settings...",
            _isRunning);
        ApplyKeyRepeatSettings();
    }

    internal void ApplyKeyRepeatSettings()
    {
        var settings = _isRunning ? _keyRepeatSettings.FastMode : _keyRepeatSettings.Default;
        _logger.LogInformation("Applying key repeat settings: {@Settings}", settings);
        SetKeyboardRepeat(settings.RepeatSpeed, settings.RepeatDelay);
    }

    private void SetKeyboardRepeat(int repeatSpeed, int repeatDelay)
    {
        const uint spiSetkeyboardspeed = 0x000B;
        const uint spiSetkeyboarddelay = 0x0017;

        try
        {
            if (!NativeMethods.SystemParametersInfo(spiSetkeyboardspeed, (uint)repeatSpeed, 0, 0))
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to set keyboard repeat speed.");

            if (!NativeMethods.SystemParametersInfo(spiSetkeyboarddelay, (uint)(repeatDelay / 250), 0, 0))
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to set keyboard repeat delay.");

            _logger.LogInformation("Key repeat settings applied: Speed={Speed}, Delay={Delay}", repeatSpeed,
                repeatDelay);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply key repeat settings.");
        }
    }

    public async Task StartMonitoringAsync(CancellationToken cancellationToken = default)
    {
        await ExecuteAsync(cancellationToken);
    }
}