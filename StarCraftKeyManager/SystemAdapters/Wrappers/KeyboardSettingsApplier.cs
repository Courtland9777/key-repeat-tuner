using System.ComponentModel;
using System.Runtime.InteropServices;
using StarCraftKeyManager.Interop;
using StarCraftKeyManager.SystemAdapters.Interfaces;

namespace StarCraftKeyManager.SystemAdapters.Wrappers;

internal sealed class KeyboardSettingsApplier : IKeyboardSettingsApplier
{
    public void ApplyRepeatSettings(int repeatSpeed, int repeatDelay)
    {
        const uint SPI_SETKEYBOARDSPEED = 0x000B;
        const uint SPI_SETKEYBOARDDELAY = 0x0017;
        const uint SPIF_UPDATEINIFILE = 0x01;
        const uint SPIF_SENDCHANGE = 0x02;
        const uint flags = SPIF_UPDATEINIFILE | SPIF_SENDCHANGE;

        if (!NativeMethods.SystemParametersInfo(SPI_SETKEYBOARDSPEED, (uint)repeatSpeed, IntPtr.Zero, flags))
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to set keyboard repeat speed.");

        var delayCode = (uint)(repeatDelay / 250);
        if (delayCode > 3) delayCode = 3;

        if (!NativeMethods.SystemParametersInfo(SPI_SETKEYBOARDDELAY, delayCode, IntPtr.Zero, flags))
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to set keyboard repeat delay.");
    }
}