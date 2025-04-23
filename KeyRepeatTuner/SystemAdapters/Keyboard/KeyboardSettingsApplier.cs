using System.ComponentModel;
using System.Runtime.InteropServices;
using KeyRepeatTuner.Interop;
using KeyRepeatTuner.SystemAdapters.Interfaces;

namespace KeyRepeatTuner.SystemAdapters.Keyboard;

internal sealed class KeyboardSettingsApplier : IKeyboardSettingsApplier
{
    private const int MaxRetries = 3;
    private const int RetryDelayMs = 50;
    private readonly ILogger<KeyboardSettingsApplier> _logger;

    public KeyboardSettingsApplier(ILogger<KeyboardSettingsApplier> logger)
    {
        _logger = logger;
    }

    public void ApplyRepeatSettings(int repeatSpeed, int repeatDelay)
    {
        const uint SPI_SETKEYBOARDSPEED = 0x000B;
        const uint SPI_SETKEYBOARDDELAY = 0x0017;
        const uint flags = 0x01 | 0x02; // SPIF_UPDATEINIFILE | SPIF_SENDCHANGE

        TrySetParamWithRetry(SPI_SETKEYBOARDSPEED, (uint)repeatSpeed, "KeyboardSpeed", flags);

        var delayCode = (uint)(repeatDelay / 250);
        if (delayCode > 3) delayCode = 3;

        TrySetParamWithRetry(SPI_SETKEYBOARDDELAY, delayCode, "KeyboardDelay", flags);
    }

    private void TrySetParamWithRetry(uint action, uint value, string label, uint flags)
    {
        for (var attempt = 1; attempt <= MaxRetries; attempt++)
        {
            if (NativeMethods.SystemParametersInfo(action, value, IntPtr.Zero, flags))
                return;

            var error = Marshal.GetLastWin32Error();
            _logger.LogWarning(
                "Attempt {Attempt}/{MaxRetries} failed to set {Label} (Value={Value}). Win32Error={Error}",
                attempt, MaxRetries, label, value, error);

            if (attempt < MaxRetries)
                Thread.Sleep(RetryDelayMs);
        }

        var finalError = Marshal.GetLastWin32Error();

        _logger.LogCritical(
            "Giving up on setting {Label} (Value={Value}) after {MaxRetries} attempts. Final Win32Error={Error}",
            label, value, MaxRetries, finalError);

        throw new Win32Exception(finalError, $"Failed to set {label} (value={value}) after {MaxRetries} attempts.");
    }
}