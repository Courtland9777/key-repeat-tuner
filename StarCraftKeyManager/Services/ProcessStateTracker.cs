using MediatR;
using StarCraftKeyManager.Events;
using StarCraftKeyManager.Interfaces;

namespace StarCraftKeyManager.Services;

public sealed class ProcessStateTracker : INotificationHandler<ProcessStarted>, INotificationHandler<ProcessStopped>
{
    private readonly HashSet<int> _activeProcesses = [];
    private readonly IKeyRepeatSettingsService _keyRepeatSettingsService;
    private readonly ILogger<ProcessStateTracker> _logger;
    private bool _lastKnownRunningState;

    public ProcessStateTracker(
        ILogger<ProcessStateTracker> logger,
        IKeyRepeatSettingsService keyRepeatSettingsService)
    {
        _logger = logger;
        _keyRepeatSettingsService = keyRepeatSettingsService;
    }

    public Task Handle(ProcessStarted notification, CancellationToken cancellationToken)
    {
        _activeProcesses.Add(notification.ProcessId);
        Log.ProcessStarted(_logger, notification.ProcessId, notification.ProcessName, null);
        return OnRunningStateChangedIfNeeded();
    }

    public Task Handle(ProcessStopped notification, CancellationToken cancellationToken)
    {
        _activeProcesses.Remove(notification.ProcessId);
        Log.ProcessStopped(_logger, notification.ProcessId, notification.ProcessName, null);
        return OnRunningStateChangedIfNeeded();
    }

    private Task OnRunningStateChangedIfNeeded()
    {
        var current = _activeProcesses.Count > 0;

        if (_lastKnownRunningState == current)
            return Task.CompletedTask;

        _lastKnownRunningState = current;
        Log.RunningStateChanged(_logger, current, null);
        _keyRepeatSettingsService.UpdateRunningState(current);

        return Task.CompletedTask;
    }

    private static class Log
    {
        public static readonly Action<ILogger, bool, Exception?> RunningStateChanged =
            LoggerMessage.Define<bool>(
                LogLevel.Information,
                new EventId(3001, nameof(RunningStateChanged)),
                "Process running state changed to: {IsRunning}");

        public static readonly Action<ILogger, int, string, Exception?> ProcessStarted =
            LoggerMessage.Define<int, string>(
                LogLevel.Debug,
                new EventId(3002, nameof(ProcessStarted)),
                "Process started: PID={Pid}, Name={Name}");

        public static readonly Action<ILogger, int, string, Exception?> ProcessStopped =
            LoggerMessage.Define<int, string>(
                LogLevel.Debug,
                new EventId(3003, nameof(ProcessStopped)),
                "Process stopped: PID={Pid}, Name={Name}");
    }
}