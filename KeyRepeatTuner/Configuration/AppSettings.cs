using KeyRepeatTuner.Configuration.ValueObjects;

namespace KeyRepeatTuner.Configuration;

public class AppSettings
{
    public required List<ProcessName> ProcessNames { get; set; }

    public required KeyRepeatSettings KeyRepeat { get; set; }
}