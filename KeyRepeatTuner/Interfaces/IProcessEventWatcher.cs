using KeyRepeatTuner.Configuration;

namespace KeyRepeatTuner.Interfaces;

public interface IProcessEventWatcher : IDisposable
{
    void Configure(string processName);
    void Start();
    void Stop();
    void OnSettingsChanged(AppSettings newSettings);
}