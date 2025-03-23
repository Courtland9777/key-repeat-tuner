using System.Diagnostics.Eventing.Reader;
using StarCraftKeyManager.Interfaces;

namespace StarCraftKeyManager.Services;

public class EventWatcherFactory : IEventWatcherFactory
{
    public EventLogWatcher Create(EventLogQuery query)
    {
        return new EventLogWatcher(query);
    }
}