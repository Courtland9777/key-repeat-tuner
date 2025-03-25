using System.Diagnostics.Eventing.Reader;
using StarCraftKeyManager.Interfaces;
using StarCraftKeyManager.SystemAdapters.Wrappers;

namespace StarCraftKeyManager.Services;

internal sealed class EventWatcherFactory : IEventWatcherFactory
{
    public IWrappedEventLogWatcher Create(EventLogQuery query)
    {
        var watcher = new EventLogWatcher(query);
        return new WrappedEventLogWatcher(watcher);
    }
}