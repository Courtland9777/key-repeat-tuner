namespace KeyRepeatTuner.Configuration.Dto;

internal sealed record AppSettingsDto
{
    public required List<string> ProcessNames { get; init; }
    public required KeyRepeatSettings KeyRepeat { get; init; }
}