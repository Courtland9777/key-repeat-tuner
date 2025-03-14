using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Options;
using StarCraftKeyManager.Interfaces;
using StarCraftKeyManager.Interop;
using StarCraftKeyManager.Models;

namespace StarCraftKeyManager.Services;

internal sealed class ProcessMonitorService : BackgroundService, IProcessMonitorService
{
    private readonly ILogger<ProcessMonitorService> _logger;
    private readonly IProcessEventWatcher _processEventWatcher;
    private readonly HashSet<int> _trackedProcesses = [];
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

        UpdateInitialProcessState();
        ApplyKeyRepeatSettings();

        try
        {
            while (!stoppingToken.IsCancellationRequested)
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
        }
    }

    private void UpdateInitialProcessState()
    {
        var sanitizedProcessName = _processName.Replace(".exe", "", StringComparison.OrdinalIgnoreCase);
        var initialProcesses = Process.GetProcessesByName(sanitizedProcessName);

        foreach (var process in initialProcesses)
            _trackedProcesses.Add(process.Id);

        _logger.LogInformation("Initial tracked processes: {Count}", _trackedProcesses.Count);
        _isRunning = _trackedProcesses.Count > 0;
    }

    private void OnProcessEventOccurred(object? sender, ProcessEventArgs e)
    {
        _logger.LogInformation("Process event occurred: {EventId} for PID {ProcessId}", e.EventId, e.ProcessId);

        switch (e.EventId)
        {
            case 4688:
                _trackedProcesses.Add(e.ProcessId);
                break;
            case 4689:
                _trackedProcesses.Remove(e.ProcessId);
                break;
        }

        var isRunningNow = _trackedProcesses.Count > 0;

        if (_isRunning == isRunningNow) return;
        _isRunning = isRunningNow;
        ApplyKeyRepeatSettings();
    }

    private void ApplyKeyRepeatSettings()
    {
        var settings = _isRunning
            ? _keyRepeatSettings.FastMode
            : _keyRepeatSettings.Default;

        _logger.LogInformation("Applying key repeat settings: {@Settings}", settings);

        SetKeyboardRepeat(settings.RepeatSpeed, settings.RepeatDelay);
    }

    private void SetKeyboardRepeat(int repeatSpeed, int repeatDelay)
    {
        const uint SPI_SETKEYBOARDSPEED = 0x000B;
        const uint SPI_SETKEYBOARDDELAY = 0x0017;

        if (!NativeMethods.SystemParametersInfo(SPI_SETKEYBOARDSPEED, (uint)repeatSpeed, 0, 0))
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to set keyboard repeat speed.");

        if (!NativeMethods.SystemParametersInfo(SPI_SETKEYBOARDDELAY, (uint)repeatDelay / 250, 0, 0))
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to set keyboard repeat delay.");

        _logger.LogInformation("Key repeat settings successfully applied: Speed={Speed}, Delay={Delay}",
            repeatSpeed, repeatDelay);
    }
}