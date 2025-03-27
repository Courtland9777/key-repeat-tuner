using System.Diagnostics.Eventing.Reader;
using StarCraftKeyManager.Interfaces;

namespace StarCraftKeyManager.Tests.TestUtilities.Fakes;

public sealed class CountingWrappedWatcher : IWrappedEventLogWatcher
{
    public int AttachCount { get; private set; }

    public bool Enabled { get; set; }

    public event EventHandler<EventRecordWrittenEventArgs>? EventRecordWritten
    {
        add => AttachCount++;
        remove { }
    }

    public void Dispose()
    {
    }
}