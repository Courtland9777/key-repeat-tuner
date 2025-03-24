using System.Diagnostics.Eventing.Reader;
using System.Reflection;

namespace StarCraftKeyManager.Tests.TestHelpers;

public static class FakeEventRecordFactory
{
    public static EventRecordWrittenEventArgs WrapStartEvent(string processName)
    {
        return CreateWrappedArgs(4688, processName);
    }

    public static EventRecordWrittenEventArgs WrapStopEvent(string processName)
    {
        return CreateWrappedArgs(4689, processName);
    }

    public static EventRecordWrittenEventArgs CreateWrappedArgs(int eventId, string processName)
    {
        var record = new FakeEventRecord(eventId, processName);

        var ctor = typeof(EventRecordWrittenEventArgs)
            .GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)
            .FirstOrDefault(c =>
            {
                var parameters = c.GetParameters();
                return parameters.Length == 1 && parameters[0].ParameterType == typeof(EventRecord);
            });

        if (ctor == null)
            throw new InvalidOperationException("Could not find suitable EventRecordWrittenEventArgs constructor");

        return (EventRecordWrittenEventArgs)ctor.Invoke(new object[] { record });
    }
}