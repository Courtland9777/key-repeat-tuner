namespace StarCraftKeyManager.Events;

public class ProcessEventArgs : EventArgs
{
    public ProcessEventArgs(ProcessEventId eventId, int processId, string processName)
    {
        EventId = eventId;
        ProcessId = processId;
        ProcessName = processName;
    }

#if DEBUG
    // Test-only constructor for invalid event scenarios
    public ProcessEventArgs(int rawEventId, int processId, string processName)
    {
        EventId = (ProcessEventId)rawEventId;
        ProcessId = processId;
        ProcessName = processName;
    }
#endif

    public ProcessEventId EventId { get; }
    public int ProcessId { get; }
    public string ProcessName { get; }
}