using StarCraftKeyManager.SystemAdapters.Interfaces;

namespace StarCraftKeyManager.SystemAdapters.Wrappers;

public class ManagementEventWatcherFactory : IManagementEventWatcherFactory
{
    public IManagementEventWatcher Create(string wqlQuery)
    {
        return new ManagementEventWatcherAdapter(wqlQuery);
    }
}