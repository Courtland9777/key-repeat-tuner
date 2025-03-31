namespace StarCraftKeyManager.Utilities;

internal static class ProcessNameSanitizer
{
    public static string Normalize(string processName)
    {
        if (string.IsNullOrWhiteSpace(processName))
            throw new ArgumentException("Process name cannot be null or whitespace.", nameof(processName));

        var nameOnly = Path.GetFileNameWithoutExtension(processName.Trim());

        if (string.IsNullOrWhiteSpace(nameOnly))
            throw new ArgumentException("Invalid process name format.", nameof(processName));

        return nameOnly;
    }

    public static string WithExe(string processName)
    {
        return $"{Normalize(processName)}.exe";
    }
}