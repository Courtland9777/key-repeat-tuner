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
        _logger.LogInformation("ProcessStateTracker initialized. No processes tracked yet.");
    }

    public void OnProcessStarted(int processId, string processName)
    {
        if (!_activeProcesses.Add(processId))
        {
            _logger.LogDebug("Start event ignored: PID={Pid} already tracked.", processId);
            return;
        }

        _logger.LogInformation("🟢 Process STARTED: {ProcessName} (PID={Pid})", processName, processId);
        _logger.LogInformation("Active process count: {Count}", _activeProcesses.Count);

        if (_activeProcesses.Count != 1) return;
        _logger.LogInformation("Switching to FastMode → First tracked process detected.");
        _keyRepeatSettingsService.UpdateRunningState(true);
    }

    public void OnProcessStopped(int processId, string processName)
    {
        if (!_activeProcesses.Remove(processId))
        {
            _logger.LogDebug("Stop event ignored: PID={Pid} not found in active processes.", processId);
            return;
        }

        _logger.LogInformation("🔴 Process STOPPED: {ProcessName} (PID={Pid})", processName, processId);
        _logger.LogInformation("Remaining active processes: {Count}", _activeProcesses.Count);

        if (!IsRunning)
        {
            _logger.LogInformation("Switching to DefaultMode → No more tracked processes.");
            _keyRepeatSettingsService.UpdateRunningState(false);
        }
        else
        {
            _logger.LogInformation("Still in FastMode. Remaining tracked PIDs: {Pids}",
                string.Join(", ", _activeProcesses));
        }
    }
}