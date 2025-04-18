using KeyRepeatTuner.Configuration.ValueObjects;

namespace KeyRepeatTuner.Configuration;

internal class AppSettings
{
    public required List<ProcessName> ProcessNames { get; init; }

    public required KeyRepeatSettings KeyRepeat { get; init; }
}