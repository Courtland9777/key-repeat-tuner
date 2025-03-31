using System.Management;
using StarCraftKeyManager.SystemAdapters.Interfaces;

namespace StarCraftKeyManager.SystemAdapters.Wrappers;

public class EventArrivedEventArgsAdapter : IEventArrivedEventArgs
{
    private readonly EventArrivedEventArgs _args;

    public EventArrivedEventArgsAdapter(EventArrivedEventArgs args)
    {
        _args = args;
    }

    public int GetProcessId()
    {
        return Convert.ToInt32(_args.NewEvent.Properties["ProcessID"].Value);
    }
}