using System.Management;
using KeyRepeatTuner.SystemAdapters.Interfaces;

namespace KeyRepeatTuner.SystemAdapters.WMI;

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
        try
        {
            _innerWatcher.Start();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to start underlying WMI watcher.", ex);
        }
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