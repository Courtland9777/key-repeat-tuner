using System.Text.RegularExpressions;

namespace StarCraftKeyManager.Configuration.ValueObjects;

public sealed partial class ProcessName
{
    public ProcessName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Process name cannot be null or whitespace.", nameof(name));

        var sanitized = Path.GetFileNameWithoutExtension(name.Trim());

        if (!ValidPattern().IsMatch(sanitized))
            throw new ArgumentException($"Invalid process name format: '{name}'", nameof(name));

        Value = sanitized;
    }

    public string Value { get; }

    [GeneratedRegex("^[a-zA-Z0-9_-]+$")]
    public static partial Regex ValidPattern();

    public override string ToString()
    {
        return Value;
    }

    public static implicit operator string(ProcessName pn)
    {
        return pn.Value;
    }

    public static implicit operator ProcessName(string name)
    {
        return new ProcessName(name);
    }
}