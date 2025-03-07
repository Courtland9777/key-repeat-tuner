namespace StarCraftKeyManager.Models;

public class KeyRepeatSettings
{
    public KeyRepeatState Default { get; set; } = new()
    {
        RepeatSpeed = 31,
        RepeatDelay = 1000
    };
    public KeyRepeatState FastMode { get; set; } = new()
    {
        RepeatSpeed = 20,
        RepeatDelay = 500
    };
}