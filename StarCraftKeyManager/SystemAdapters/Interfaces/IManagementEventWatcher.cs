using System.Management;

namespace KeyRepeatTuner.SystemAdapters.Interfaces;

public interface IManagementEventWatcher : IDisposable
{
    event EventArrivedEventHandler EventArrived;

    void Start();
    void Stop();
}