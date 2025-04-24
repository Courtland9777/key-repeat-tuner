using KeyRepeatTuner.Configuration.Dto;
using KeyRepeatTuner.Configuration.ValueObjects;

namespace KeyRepeatTuner.Configuration.Mapping;

internal static class AppSettingsMapper
{
    public static AppSettings ToDomain(AppSettingsDto dto)
    {
        if (dto.ProcessNames == null || dto.ProcessNames.Any(string.IsNullOrWhiteSpace))
            throw new InvalidOperationException("ProcessNames must be non-null and non-empty.");

        return new AppSettings
        {
            ProcessNames = [.. dto.ProcessNames.Select(name => new ProcessName(name))],
            KeyRepeat = dto.KeyRepeat ?? throw new InvalidOperationException("KeyRepeat settings missing")
        };
    }
}