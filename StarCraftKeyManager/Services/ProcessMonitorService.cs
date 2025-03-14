using System.Collections.Concurrent;
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
    private readonly object _processLock = new();
    private readonly ConcurrentDictionary<int, bool> _trackedProcesses = new();
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
        var initialProcesses = await Task.Run(() => Process.GetProcessesByName(sanitizedProcessName), stoppingToken);
        foreach (var process in initialProcesses) _trackedProcesses.TryAdd(process.Id, true);

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
        }
    }

    private void OnProcessEventOccurred(object? sender, ProcessEventArgs e)
    {
        bool stateChanged;
        lock (_processLock)
        {
            switch (e.EventId)
            {
                case 4688:
                    _trackedProcesses.TryAdd(e.ProcessId, true);
                    break;
                case 4689:
                    _trackedProcesses.TryRemove(e.ProcessId, out _);
                    break;
            }

            var wasRunning = _isRunning;
            _isRunning = !_trackedProcesses.IsEmpty;
            stateChanged = wasRunning != _isRunning;
        }

        _logger.LogInformation("Process event occurred: {EventId} for PID {ProcessId}", e.EventId, e.ProcessId);
        if (!stateChanged) return;
        _logger.LogInformation("Process running state changed to {IsRunning}. Updating key repeat settings...",
            _isRunning);
        _ = Task.Run(() =>
        {
            try
            {
                ApplyKeyRepeatSettings();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to apply key repeat settings");
            }
        });
    }

    private void ApplyKeyRepeatSettings()
    {
        var settings = _isRunning ? _keyRepeatSettings.FastMode : _keyRepeatSettings.Default;
        _logger.LogInformation("Applying key repeat settings: {@Settings}", settings);
        SetKeyboardRepeat(settings.RepeatSpeed, settings.RepeatDelay);
    }

    private void SetKeyboardRepeat(int repeatSpeed, int repeatDelay)
    {
        // ReSharper disable once InconsistentNaming
        const uint SPI_SETKEYBOARDSPEED = 0x000B;
        // ReSharper disable once InconsistentNaming
        const uint SPI_SETKEYBOARDDELAY = 0x0017;

        if (!NativeMethods.SystemParametersInfo(SPI_SETKEYBOARDSPEED, (uint)repeatSpeed, 0, 0))
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to set keyboard repeat speed.");

        if (!NativeMethods.SystemParametersInfo(SPI_SETKEYBOARDDELAY, (uint)repeatDelay / 250, 0, 0))
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to set keyboard repeat delay.");

        _logger.LogInformation("Key repeat settings successfully applied: Speed={Speed}, Delay={Delay}", repeatSpeed,
            repeatDelay);
    }
}