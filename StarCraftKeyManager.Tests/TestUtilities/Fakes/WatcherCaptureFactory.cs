using System.Diagnostics.Eventing.Reader;
using StarCraftKeyManager.Interfaces;

namespace StarCraftKeyManager.Tests.TestUtilities.Fakes;

public sealed class WatcherCaptureFactory : IEventWatcherFactory
{
    public IWrappedEventLogWatcher? LastCreated { get; private set; }

    public IWrappedEventLogWatcher Create(EventLogQuery query)
    {
        LastCreated = new DisposableWrappedWatcher();
        return LastCreated;
    }

    private sealed class DisposableWrappedWatcher : IWrappedEventLogWatcher
    {
        public bool Enabled { get; set; }

        public event EventHandler<EventRecordWrittenEventArgs>? EventRecordWritten;

        public void Dispose()
        {
        }
    }
}