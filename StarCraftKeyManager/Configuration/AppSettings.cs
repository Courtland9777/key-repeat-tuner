namespace StarCraftKeyManager.Configuration;

public class AppSettings
{
    public required string ProcessName { get; set; }
    public required KeyRepeatSettings KeyRepeat { get; set; }
}