using System.Management;
using KeyRepeatTuner.SystemAdapters.Interfaces;

namespace KeyRepeatTuner.SystemAdapters.Wrappers;

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