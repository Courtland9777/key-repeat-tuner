using System.Collections.Concurrent;
using MediatR;
using StarCraftKeyManager.Events;
using StarCraftKeyManager.Interfaces;

namespace StarCraftKeyManager.Services;

public sealed class ProcessMonitorService : BackgroundService,
    IProcessMonitorService,
    INotificationHandler<ProcessStarted>,
    INotificationHandler<ProcessStopped>
{
    private readonly IKeyRepeatSettingsService _keyRepeatSettingsService;
    private readonly ILogger<ProcessMonitorService> _logger;
    private readonly ConcurrentDictionary<int, byte> _trackedProcesses = new();
    private bool _lastKnownRunningState;

    public ProcessMonitorService(
        ILogger<ProcessMonitorService> logger,
        IKeyRepeatSettingsService keyRepeatSettingsService)
    {
        _logger = logger;
        _keyRepeatSettingsService = keyRepeatSettingsService;
    }

    public bool IsRunning => !_trackedProcesses.IsEmpty;

    public Task Handle(ProcessStarted notification, CancellationToken cancellationToken)
    {
        _trackedProcesses.TryAdd(notification.ProcessId, 0);
        return OnRunningStateChangedIfNeeded();
    }

    public Task Handle(ProcessStopped notification, CancellationToken cancellationToken)
    {
        _trackedProcesses.TryRemove(notification.ProcessId, out _);
        return OnRunningStateChangedIfNeeded();
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting process monitor service.");
        return Task.CompletedTask;
    }

    private Task OnRunningStateChangedIfNeeded()
    {
        var current = IsRunning;

        if (_lastKnownRunningState == current)
            return Task.CompletedTask;

        _lastKnownRunningState = current;

        _logger.LogInformation("Process running state changed to: {IsRunning}", current);
        _keyRepeatSettingsService.UpdateRunningState(current);

        return Task.CompletedTask;
    }
}