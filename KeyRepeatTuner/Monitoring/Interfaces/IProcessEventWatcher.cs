using KeyRepeatTuner.Configuration;

namespace KeyRepeatTuner.Monitoring.Interfaces;

internal interface IProcessEventWatcher : IDisposable
{
    void Start();
    void Stop();
    void OnSettingsChanged(AppSettings newSettings);
}