using KeyRepeatTuner.SystemAdapters.Interfaces;
using Microsoft.Win32;

namespace KeyRepeatTuner.SystemAdapters.Keyboard;

public sealed class KeyboardRegistryReader : IKeyboardRegistryReader
{
    private const string KeyboardPath = @"HKEY_CURRENT_USER\Control Panel\Keyboard";
    private const int MaxRetries = 3;
    private const int RetryDelayMs = 50;
    private readonly ILogger<KeyboardRegistryReader> _logger;

    public KeyboardRegistryReader(ILogger<KeyboardRegistryReader> logger)
    {
        _logger = logger;
    }

    public string? GetRepeatSpeed()
    {
        return TryReadRegistryValue("KeyboardSpeed");
    }

    public string? GetRepeatDelay()
    {
        return TryReadRegistryValue("KeyboardDelay");
    }

    private string? TryReadRegistryValue(string valueName)
    {
        for (var attempt = 1; attempt <= MaxRetries; attempt++)
            try
            {
                var value = Registry.GetValue(KeyboardPath, valueName, null);
                if (value is string str)
                    return str;

                _logger.LogWarning("Registry value '{ValueName}' under '{Path}' was null or not a string.", valueName,
                    KeyboardPath);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to read registry value '{ValueName}' (attempt {Attempt}/{Max}).",
                    valueName, attempt, MaxRetries);

                if (attempt < MaxRetries)
                    Thread.Sleep(RetryDelayMs);
                else
                    _logger.LogCritical("Giving up reading '{ValueName}' after {MaxRetries} attempts.", valueName,
                        MaxRetries);
            }

        return null;
    }
}