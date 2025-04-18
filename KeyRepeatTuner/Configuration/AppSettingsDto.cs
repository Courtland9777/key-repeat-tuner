namespace KeyRepeatTuner.Configuration;

internal class AppSettingsDto
{
    public required List<string> ProcessNames { get; init; }
    public required KeyRepeatSettings KeyRepeat { get; init; }
}