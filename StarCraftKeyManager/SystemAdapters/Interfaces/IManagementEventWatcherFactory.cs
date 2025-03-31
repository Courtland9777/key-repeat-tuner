namespace StarCraftKeyManager.SystemAdapters.Interfaces;

public interface IManagementEventWatcherFactory
{
    IManagementEventWatcher Create(string wqlQuery);
}