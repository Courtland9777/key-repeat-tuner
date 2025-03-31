using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using StarCraftKeyManager.Configuration;
using StarCraftKeyManager.Events;
using StarCraftKeyManager.Interfaces;
using StarCraftKeyManager.SystemAdapters.Interfaces;
using StarCraftKeyManager.Utilities;

namespace StarCraftKeyManager.Services;

internal sealed class ProcessMonitorService : BackgroundService, IProcessMonitorService
{
    private readonly IKeyboardSettingsApplier _keyboardSettingsApplier;
    private readonly ILogger<ProcessMonitorService> _logger;
    private readonly IProcessEventWatcher _processEventWatcher;
    private readonly IProcessProvider _processProvider;
    private readonly ConcurrentDictionary<int, byte> _trackedProcesses = new();

    private bool _isRunning;
    private KeyRepeatSettings _keyRepeatSettings;
    private string _processName;

    public ProcessMonitorService(
        ILogger<ProcessMonitorService> logger,
        IOptionsMonitor<AppSettings> optionsMonitor,
        IProcessEventWatcher processEventWatcher,
        IKeyboardSettingsApplier keyboardSettingsApplier,
        IProcessProvider processProvider)
    {
        _logger = logger;
        _processEventWatcher = processEventWatcher;
        _keyboardSettingsApplier = keyboardSettingsApplier;
        _processProvider = processProvider;

        var settings = optionsMonitor.CurrentValue;
        _processName = ProcessNameSanitizer.Normalize(settings.ProcessMonitor.ProcessName);
        _keyRepeatSettings = settings.KeyRepeat;

        _processEventWatcher.Configure(_processName);
        _processEventWatcher.ProcessEventOccurred += OnProcessEventOccurred;

        optionsMonitor.OnChange(updatedSettings =>
        {
            _logger.LogInformation("Configuration updated: {@Settings}", updatedSettings);
            _processName = ProcessNameSanitizer.Normalize(updatedSettings.ProcessMonitor.ProcessName);
            _keyRepeatSettings = updatedSettings.KeyRepeat;
            _processEventWatcher.Configure(_processName);
            ApplyKeyRepeatSettings();
        });
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _logger.LogInformation("Starting process monitor service.");
            _processEventWatcher.Start();
            _logger.LogInformation("Applying key repeat settings for {ProcessName}",
                ProcessNameSanitizer.WithExe(_processName));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start process watcher.");
        }

        var sanitizedProcessName = _processName.Replace(".exe", "", StringComparison.OrdinalIgnoreCase);
        var initialProcessIds = _processProvider.GetProcessIdsByName(sanitizedProcessName);
        foreach (var pid in initialProcessIds)
            _trackedProcesses.TryAdd(pid, 0);

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
            default:
                _logger.LogInformation("Unrelated process event occurred: {EventId} for PID {ProcessId}.", e.EventId,
                    e.ProcessId);
                return;
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
        try
        {
            var settings = _isRunning ? _keyRepeatSettings.FastMode : _keyRepeatSettings.Default;

            _logger.LogInformation(
                "Applying key repeat settings: Mode={Mode}, Speed={Speed}, Delay={Delay} for {ProcessName}",
                _isRunning ? "FastMode" : "Default",
                settings.RepeatSpeed,
                settings.RepeatDelay,
                ProcessNameSanitizer.WithExe(_processName));


            _keyboardSettingsApplier.ApplyRepeatSettings(settings.RepeatSpeed, settings.RepeatDelay);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply key repeat settings. Running={IsRunning}", _isRunning);
        }
    }
}