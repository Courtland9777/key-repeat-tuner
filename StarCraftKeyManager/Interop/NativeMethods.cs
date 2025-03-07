using System.Runtime.InteropServices;

namespace StarCraftKeyManager.Interop;

internal static partial class NativeMethods
{
    private const string User32 = "user32.dll";

    [LibraryImport(User32, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool SystemParametersInfo(uint action, uint param, uint vparam, uint init);
}