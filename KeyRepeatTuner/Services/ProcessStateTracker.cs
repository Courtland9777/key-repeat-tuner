using KeyRepeatTuner.Events;
using KeyRepeatTuner.Interfaces;
using MediatR;

namespace KeyRepeatTuner.Services;

public sealed class ProcessStateTracker : INotificationHandler<ProcessStarted>, INotificationHandler<ProcessStopped>
{
    private readonly HashSet<int> _activeProcesses = [];
    private readonly IKeyRepeatSettingsService _keyRepeatSettingsService;
    private readonly ILogger<ProcessStateTracker> _logger;

    public ProcessStateTracker(
        ILogger<ProcessStateTracker> logger,
        IKeyRepeatSettingsService keyRepeatSettingsService)
    {
        _logger = logger;
        _keyRepeatSettingsService = keyRepeatSettingsService;
    }

    private bool IsRunning => _activeProcesses.Count > 0;

    public Task Handle(ProcessStarted notification, CancellationToken cancellationToken)
    {
        _activeProcesses.Add(notification.ProcessId);

        _logger.LogDebug("Process started: PID={Pid}, Name={Name}", notification.ProcessId, notification.ProcessName);

        if (_activeProcesses.Count == 1) _keyRepeatSettingsService.UpdateRunningState(IsRunning);

        return Task.CompletedTask;
    }

    public Task Handle(ProcessStopped notification, CancellationToken cancellationToken)
    {
        _activeProcesses.Remove(notification.ProcessId);

        _logger.LogDebug("Process stopped: PID={Pid}, Name={Name}", notification.ProcessId, notification.ProcessName);

        if (!IsRunning)
            _keyRepeatSettingsService.UpdateRunningState(false);
        else
            _logger.LogInformation("Still in FastMode. Other processes still active.");

        return Task.CompletedTask;
    }
}