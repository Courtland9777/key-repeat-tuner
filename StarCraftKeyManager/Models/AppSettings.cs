namespace StarCraftKeyManager.Models;

public class AppSettings
{
    public ProcessMonitorSettings ProcessMonitor { get; set; } = new();
    public required KeyRepeatSettings KeyRepeat { get; set; }
}