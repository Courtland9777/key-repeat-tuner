using System.Diagnostics.Eventing.Reader;
using StarCraftKeyManager.Adapters;

namespace StarCraftKeyManager.Wrappers;

internal sealed class WrappedEventRecordWrittenEventArgs : IWrappedEventRecordWrittenEventArgs
{
    public WrappedEventRecordWrittenEventArgs(EventRecordWrittenEventArgs original)
    {
        EventRecord = original.EventRecord != null ? new WrappedEventRecord(original.EventRecord) : null;
    }

    public IWrappedEventRecord? EventRecord { get; }
}