using StarCraftKeyManager.Adapters;

namespace StarCraftKeyManager.Tests.TestUtilities.Fakes;

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