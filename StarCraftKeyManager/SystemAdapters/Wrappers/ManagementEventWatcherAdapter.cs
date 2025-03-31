using System.Management;
using StarCraftKeyManager.SystemAdapters.Interfaces;

namespace StarCraftKeyManager.SystemAdapters.Wrappers;

public class ManagementEventWatcherAdapter : IManagementEventWatcher
{
    private readonly ManagementEventWatcher _innerWatcher;

    public ManagementEventWatcherAdapter(string query)
    {
        _innerWatcher = new ManagementEventWatcher(new WqlEventQuery(query));
    }

    public event EventArrivedEventHandler? EventArrived
    {
        add => _innerWatcher.EventArrived += value;
        remove => _innerWatcher.EventArrived -= value;
    }

    public void Start()
    {
        _innerWatcher.Start();
    }

    public void Stop()
    {
        _innerWatcher.Stop();
    }

    public void Dispose()
    {
        _innerWatcher.Dispose();
        GC.SuppressFinalize(this);
    }
}