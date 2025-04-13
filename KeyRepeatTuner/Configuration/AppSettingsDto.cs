namespace KeyRepeatTuner.Configuration;

public class AppSettingsDto
{
    public required List<string>? ProcessNames { get; set; }
    public required KeyRepeatSettings? KeyRepeat { get; set; }
}