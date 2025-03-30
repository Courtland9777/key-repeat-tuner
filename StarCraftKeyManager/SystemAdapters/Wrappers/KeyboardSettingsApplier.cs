using System.ComponentModel;
using System.Runtime.InteropServices;
using StarCraftKeyManager.Interop;
using StarCraftKeyManager.SystemAdapters.Interfaces;

namespace StarCraftKeyManager.SystemAdapters.Wrappers;

internal sealed class KeyboardSettingsApplier : IKeyboardSettingsApplier
{
    public void ApplyRepeatSettings(int repeatSpeed, int repeatDelay)
    {
        const uint spiSetkeyboardspeed = 0x000B;
        const uint spiSetkeyboarddelay = 0x0017;

        if (!NativeMethods.SystemParametersInfo(spiSetkeyboardspeed, (uint)repeatSpeed, 0, 0))
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to set keyboard repeat speed.");

        if (!NativeMethods.SystemParametersInfo(spiSetkeyboarddelay, (uint)(repeatDelay / 250), 0, 0))
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to set keyboard repeat delay.");
    }
}