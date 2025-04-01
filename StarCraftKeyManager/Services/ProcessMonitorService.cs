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
    private readonly IKeyRepeatSettingsService _keyRepeatService;
    private readonly ILogger<ProcessMonitorService> _logger;
    private readonly IProcessEventWatcher _processEventWatcher;
    private readonly IProcessProvider _processProvider;
    private readonly ConcurrentDictionary<int, byte> _trackedProcesses = new();
    private string _processName;

    public ProcessMonitorService(
        ILogger<ProcessMonitorService> logger,
        IOptionsMonitor<AppSettings> optionsMonitor,
        IProcessEventWatcher processEventWatcher,
        IProcessProvider processProvider,
        IKeyRepeatSettingsService keyRepeatService)
    {
        _logger = logger;
        _processEventWatcher = processEventWatcher;
        _processProvider = processProvider;
        _keyRepeatService = keyRepeatService;

        var settings = optionsMonitor.CurrentValue;
        _processName = ProcessNameSanitizer.Normalize(settings.ProcessMonitor.ProcessName);

        _processEventWatcher.Configure(_processName);
        _processEventWatcher.ProcessEventOccurred += OnProcessEventOccurred;

        optionsMonitor.OnChange(updatedSettings =>
        {
            _logger.LogInformation("Configuration updated: {@Settings}", updatedSettings);
            _processName = ProcessNameSanitizer.Normalize(updatedSettings.ProcessMonitor.ProcessName);
            _processEventWatcher.Configure(_processName);
            _keyRepeatService.UpdateRunningState(IsRunning);
        });
    }

    private bool IsRunning => !_trackedProcesses.IsEmpty;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _logger.LogInformation("Starting process monitor service.");
            _processEventWatcher.Start();
            _logger.LogInformation("Monitoring process: {ProcessName}",
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

        _logger.LogInformation("Initial tracked processes: {Count}", _trackedProcesses.Count);
        _keyRepeatService.UpdateRunningState(IsRunning);

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
        HandleProcessEvent(e);
    }

    internal void HandleProcessEvent(ProcessEventArgs e)
    {
        var wasRunning = IsRunning;

        switch (e.EventId)
        {
            case ProcessEventId.Start:
                _trackedProcesses.TryAdd(e.ProcessId, 0);
                break;
            case ProcessEventId.Stop:
                _trackedProcesses.TryRemove(e.ProcessId, out _);
                break;
            default:
                _logger.LogInformation("Unrelated process event occurred: {EventId} for PID {ProcessId}.", e.EventId,
                    e.ProcessId);
                return;
        }

        if (wasRunning == IsRunning) return;

        _logger.LogInformation("Process running state changed to {IsRunning}. Updating key repeat settings...",
            IsRunning);
        _keyRepeatService.UpdateRunningState(IsRunning);
    }
}