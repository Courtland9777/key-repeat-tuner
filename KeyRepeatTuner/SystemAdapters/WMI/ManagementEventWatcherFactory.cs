using KeyRepeatTuner.SystemAdapters.Interfaces;

namespace KeyRepeatTuner.SystemAdapters.WMI;

public class ManagementEventWatcherFactory : IManagementEventWatcherFactory
{
    public IManagementEventWatcher Create(string wqlQuery)
    {
        return new ManagementEventWatcherAdapter(wqlQuery);
    }
}