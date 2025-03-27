using System.Diagnostics.Eventing.Reader;
using StarCraftKeyManager.Interfaces;

namespace StarCraftKeyManager.Tests.TestUtilities.Fakes;

public sealed class TrackingWrappedWatcher : IWrappedEventLogWatcher
{
    public bool Unsubscribed { get; private set; }
    public bool Enabled { get; set; }

    public event EventHandler<EventRecordWrittenEventArgs>? EventRecordWritten
    {
        add { }
        remove => Unsubscribed = true;
    }

    public void Dispose()
    {
    }
}