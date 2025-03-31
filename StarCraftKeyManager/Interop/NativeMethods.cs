using System.Runtime.InteropServices;

namespace StarCraftKeyManager.Interop;

internal static partial class NativeMethods
{
    [LibraryImport("user32.dll", EntryPoint = "SystemParametersInfoW", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    [SuppressGCTransition]
    internal static partial bool SystemParametersInfo(
        uint uiAction,
        uint uiParam,
        IntPtr pvParam,
        uint fWinIni);
}