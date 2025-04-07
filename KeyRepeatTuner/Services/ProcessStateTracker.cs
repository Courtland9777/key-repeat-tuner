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
        if (IsRunning)
        {
            _activeProcesses.Add(notification.ProcessId);
            Log.ProcessStarted(_logger, notification.ProcessId, notification.ProcessName, null);
            return Task.CompletedTask;
        }

        _activeProcesses.Add(notification.ProcessId);
        Log.ProcessStarted(_logger, notification.ProcessId, notification.ProcessName, null);
        _keyRepeatSettingsService.UpdateRunningState(IsRunning);
        return Task.CompletedTask;
    }

    public Task Handle(ProcessStopped notification, CancellationToken cancellationToken)
    {
        _activeProcesses.Remove(notification.ProcessId);
        Log.ProcessStopped(_logger, notification.ProcessId, notification.ProcessName, null);
        if (IsRunning)
        {
            _logger.LogInformation("Still in FastMode. Other processes still active.");
            return Task.CompletedTask;
        }

        _keyRepeatSettingsService.UpdateRunningState(IsRunning);
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