namespace StarCraftKeyManager.Models;

public class KeyRepeatState
{
    public int RepeatSpeed { get; set; } = 31;  // Default Windows value
    public int RepeatDelay { get; set; } = 1000; // Default Windows value (1 sec)
}