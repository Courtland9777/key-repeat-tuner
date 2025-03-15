namespace StarCraftKeyManager.Models;

public class ProcessEventArgs : EventArgs
{
    public ProcessEventArgs(int eventId, int processId, string processName = "Unknown")
    {
        EventId = eventId;
        ProcessId = processId;
        ProcessName = processName;
    }

    public int EventId { get; }
    public int ProcessId { get; }
    public string ProcessName { get; }
}