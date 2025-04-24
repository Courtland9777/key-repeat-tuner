namespace KeyRepeatTuner.Monitoring.Interfaces;

public interface IProcessEventRouter
{
    bool IsRunning { get; }
    void OnProcessStarted(int processId, string processName);
    void OnProcessStopped(int processId, string processName);
}