namespace StarCraftKeyManager.Models;

public class KeyRepeatSettings
{
    public KeyRepeatState Default { get; init; } = new()
    {
        RepeatSpeed = 31,
        RepeatDelay = 1000
    };

    public KeyRepeatState FastMode { get; init; } = new()
    {
        RepeatSpeed = 20,
        RepeatDelay = 500
    };
}