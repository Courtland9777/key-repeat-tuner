using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace KeyRepeatTuner.Configuration.ValueObjects;

internal readonly partial record struct ProcessName
{
    private static readonly Regex ValidPatternRegex = ValidPattern();

    private static readonly ConcurrentDictionary<string, string> ValidatedNames = new();

    public ProcessName(string name)
    {
        Value = Normalize(name);
    }

    public string Value { get; }

    public string WithExe()
    {
        return $"{Value}.exe";
    }

    public override string ToString()
    {
        return Value;
    }

    [GeneratedRegex("^[a-zA-Z0-9_-]+$")]
    private static partial Regex ValidPattern();

    private static string Normalize(string processName)
    {
        if (string.IsNullOrWhiteSpace(processName))
            throw new ArgumentException("Process name cannot be null or whitespace.", nameof(processName));

        var nameOnly = Path.GetFileNameWithoutExtension(processName.Trim());

        return ValidatedNames.GetOrAdd(nameOnly, static key =>
        {
            if (!ValidPatternRegex.IsMatch(key))
                throw new ArgumentException($"Invalid process name format: '{key}'", nameof(processName));

            return key;
        });
    }
}