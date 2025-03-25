using StarCraftKeyManager.Adapters;

namespace StarCraftKeyManager.Tests.TestHelpers;

public static class FakeEventRecordFactory
{
    public static IWrappedEventRecordWrittenEventArgs WrapStartEvent(string processName)
    {
        return new FakeWrappedEventRecordWrittenEventArgs(4688, processName);
    }

    public static IWrappedEventRecordWrittenEventArgs WrapStopEvent(string processName)
    {
        return new FakeWrappedEventRecordWrittenEventArgs(4689, processName);
    }
}