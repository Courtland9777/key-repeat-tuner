using System.Management;
using System.Reflection;

namespace StarCraftKeyManager.Tests.TestUtilities.Fakes;

public static class TestEventFactory
{
    public static EventArrivedEventArgs CreateProcessEvent(int processId)
    {
        var mbo = new ManagementClass("Win32_Process").CreateInstance();
        mbo["ProcessID"] = processId;

        var args = (EventArrivedEventArgs)Activator.CreateInstance(
            typeof(EventArrivedEventArgs),
            true)!;

        typeof(EventArrivedEventArgs)
            .GetField("newEvent", BindingFlags.NonPublic | BindingFlags.Instance)!
            .SetValue(args, mbo);

        return args;
    }
}