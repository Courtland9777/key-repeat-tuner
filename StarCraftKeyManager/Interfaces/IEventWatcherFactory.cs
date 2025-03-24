using System.Diagnostics.Eventing.Reader;

namespace StarCraftKeyManager.Interfaces;

public interface IEventWatcherFactory
{
    IWrappedEventLogWatcher Create(EventLogQuery query);
}