namespace StarCraftKeyManager.Models;

public class KeyRepeatState
{
    public int RepeatSpeed { get; init; } = 31; // Default Windows value
    public int RepeatDelay { get; init; } = 1000; // Default Windows value (1 sec)
}