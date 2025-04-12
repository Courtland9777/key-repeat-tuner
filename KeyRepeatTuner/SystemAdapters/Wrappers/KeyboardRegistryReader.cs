using KeyRepeatTuner.SystemAdapters.Interfaces;
using Microsoft.Win32;

namespace KeyRepeatTuner.SystemAdapters.Wrappers;

public class KeyboardRegistryReader : IKeyboardRegistryReader
{
    private const string Key = @"HKEY_CURRENT_USER\Control Panel\Keyboard";

    public string? GetRepeatSpeed()
    {
        return Registry.GetValue(Key, "KeyboardSpeed", null)?.ToString();
    }

    public string? GetRepeatDelay()
    {
        return Registry.GetValue(Key, "KeyboardDelay", null)?.ToString();
    }
}