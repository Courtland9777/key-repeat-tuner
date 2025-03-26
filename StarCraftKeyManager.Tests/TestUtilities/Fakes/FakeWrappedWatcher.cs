using System.Diagnostics.Eventing.Reader;
using StarCraftKeyManager.Interfaces;

namespace StarCraftKeyManager.Tests.TestUtilities.Fakes;

public class FakeWrappedWatcher : IEventWatcherFactory
{
    public IWrappedEventLogWatcher Create(EventLogQuery query)
    {
        return new NoOpWrappedEventWatcher();
    }

    private class NoOpWrappedEventWatcher : IWrappedEventLogWatcher
    {
        public bool Enabled { get; set; }

        public event EventHandler<EventRecordWrittenEventArgs>? EventRecordWritten;

        public void Dispose()
        {
        }
    }
}