using System.Diagnostics.Eventing.Reader;
using StarCraftKeyManager.Interfaces;

namespace StarCraftKeyManager.Tests.TestUtilities.Fakes;

public sealed class TestWatcherFactory : IEventWatcherFactory
{
    private readonly IWrappedEventLogWatcher _watcher;

    public TestWatcherFactory(IWrappedEventLogWatcher watcher)
    {
        _watcher = watcher;
    }

    public IWrappedEventLogWatcher Create(EventLogQuery query)
    {
        return _watcher;
    }
}