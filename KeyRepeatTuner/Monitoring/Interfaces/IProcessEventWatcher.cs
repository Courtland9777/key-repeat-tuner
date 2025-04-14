using KeyRepeatTuner.Configuration;

namespace KeyRepeatTuner.Monitoring.Interfaces;

public interface IProcessEventWatcher : IDisposable
{
    void Start();
    void Stop();
    void OnSettingsChanged(AppSettings newSettings);
}