using System.Diagnostics.Eventing.Reader;

namespace StarCraftKeyManager.Interfaces;

public interface IEventWatcherFactory
{
    EventLogWatcher Create(EventLogQuery query);
}