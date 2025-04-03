using System.Text.RegularExpressions;

namespace KeyRepeatTuner.Configuration.ValueObjects;

public sealed partial class ProcessName
{
    public ProcessName(string name)
    {
        Value = Normalize(name);
    }

    public string Value { get; }

    public override string ToString()
    {
        return Value ?? throw new InvalidOperationException("ProcessName is not initialized.");
    }

    public static implicit operator string(ProcessName pn)
    {
        return pn.Value;
    }

    public static implicit operator ProcessName(string name)
    {
        return new ProcessName(name);
    }

    [GeneratedRegex("^[a-zA-Z0-9_-]+$")]
    public static partial Regex ValidPattern();

    private static string Normalize(string processName)
    {
        if (string.IsNullOrWhiteSpace(processName))
            throw new ArgumentException("Process name cannot be null or whitespace.", nameof(processName));

        var nameOnly = Path.GetFileNameWithoutExtension(processName.Trim());

        if (!ValidPattern().IsMatch(nameOnly))
            throw new ArgumentException($"Invalid process name format: '{processName}'", nameof(processName));

        return nameOnly;
    }

    public string WithExe()
    {
        return $"{Value}.exe";
    }
}