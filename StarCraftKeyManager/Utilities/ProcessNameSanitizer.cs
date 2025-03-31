namespace StarCraftKeyManager.Utilities;

internal static class ProcessNameSanitizer
{
    public static string Normalize(string processName)
    {
        if (string.IsNullOrWhiteSpace(processName))
            throw new ArgumentException("Process name cannot be null or empty.", nameof(processName));

        var trimmed = processName.Trim();
        var nameOnly = Path.GetFileNameWithoutExtension(trimmed);

        if (string.IsNullOrWhiteSpace(nameOnly))
            throw new ArgumentException("Invalid process name format.", nameof(processName));

        return nameOnly;
    }

    public static string WithExe(string processName)
    {
        return $"{Normalize(processName)}.exe";
    }
}