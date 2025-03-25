using StarCraftKeyManager.Adapters;

namespace StarCraftKeyManager.Tests.TestUtilities.Fakes;

public class FakeWrappedEventRecordWrittenEventArgs : IWrappedEventRecordWrittenEventArgs
{
    public FakeWrappedEventRecordWrittenEventArgs(int eventId, string processName)
    {
        EventRecord = new FakeWrappedEventRecord(eventId, processName);
    }

    public FakeWrappedEventRecordWrittenEventArgs(IWrappedEventRecord? record)
    {
        EventRecord = record;
    }

    public IWrappedEventRecord? EventRecord { get; }
}