namespace StarCraftKeyManager.Models;

public class AppSettings
{
    public ProcessMonitorSettings ProcessMonitor { get; set; } = new();
    public KeyRepeatSettings KeyRepeat { get; set; } = new();
}