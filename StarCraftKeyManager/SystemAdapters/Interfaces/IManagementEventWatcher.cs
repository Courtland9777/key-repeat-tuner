using System.Management;

namespace StarCraftKeyManager.SystemAdapters.Interfaces;

public interface IManagementEventWatcher : IDisposable
{
    event EventArrivedEventHandler EventArrived;

    void Start();
    void Stop();
}