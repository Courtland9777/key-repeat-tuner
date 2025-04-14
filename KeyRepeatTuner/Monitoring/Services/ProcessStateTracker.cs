using KeyRepeatTuner.Core.Interfaces;
using KeyRepeatTuner.Monitoring.Interfaces;

namespace KeyRepeatTuner.Monitoring.Services;

public sealed class ProcessStateTracker : IProcessEventRouter
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

    public void OnStartup()
    {
        // No-op (hook for future use)
    }

    public void OnProcessStarted(int processId, string processName)
    {
        if (!_activeProcesses.Add(processId))
        {
            _logger.LogDebug("Start event ignored: PID={Pid} already tracked.", processId);
            return;
        }

        _logger.LogDebug("Process started: PID={Pid}, Name={Name}", processId, processName);

        if (_activeProcesses.Count == 1) _keyRepeatSettingsService.UpdateRunningState(true);
    }

    public void OnProcessStopped(int processId, string processName)
    {
        if (!_activeProcesses.Remove(processId))
        {
            _logger.LogDebug("Stop event ignored: PID={Pid} not found in active processes.", processId);
            return;
        }

        _logger.LogDebug("Process stopped: PID={Pid}, Name={Name}", processId, processName);

        if (!IsRunning)
            _keyRepeatSettingsService.UpdateRunningState(false);
        else
            _logger.LogInformation("Still in FastMode. Other processes still active.");
    }
}