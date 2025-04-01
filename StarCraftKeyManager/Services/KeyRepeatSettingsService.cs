using Microsoft.Extensions.Options;
using StarCraftKeyManager.Configuration;
using StarCraftKeyManager.Interfaces;
using StarCraftKeyManager.SystemAdapters.Interfaces;

namespace StarCraftKeyManager.Services;

internal sealed class KeyRepeatSettingsService : IKeyRepeatSettingsService, IAppSettingsChangeHandler, IDisposable
{
    private readonly IKeyboardSettingsApplier _keyboardSettingsApplier;
    private readonly ILogger<KeyRepeatSettingsService> _logger;
    private KeyRepeatSettings _settings;

    public KeyRepeatSettingsService(
        ILogger<KeyRepeatSettingsService> logger,
        IKeyboardSettingsApplier keyboardSettingsApplier,
        IOptionsMonitor<AppSettings> optionsMonitor)
    {
        _logger = logger;
        _keyboardSettingsApplier = keyboardSettingsApplier;
        _settings = optionsMonitor.CurrentValue.KeyRepeat;
    }

    public void OnSettingsChanged(AppSettings newSettings)
    {
        _settings = newSettings.KeyRepeat;
        _logger.LogInformation("Key repeat settings updated via runtime coordinator.");
    }

    public void Dispose()
    {
    }

    public void UpdateRunningState(bool isRunning)
    {
        try
        {
            var mode = isRunning ? "FastMode" : "Default";
            var config = isRunning ? _settings.FastMode : _settings.Default;

            Log.ApplySettings(_logger, mode, config.RepeatSpeed, config.RepeatDelay, null);
            _keyboardSettingsApplier.ApplyRepeatSettings(config.RepeatSpeed, config.RepeatDelay);
        }
        catch (Exception ex)
        {
            Log.ApplyFailed(_logger, ex);
        }
    }

    private static class Log
    {
        public static readonly Action<ILogger, string, int, int, Exception?> ApplySettings =
            LoggerMessage.Define<string, int, int>(
                LogLevel.Information,
                new EventId(1000, nameof(ApplySettings)),
                "Applying key repeat settings: Mode={Mode}, Speed={Speed}, Delay={Delay}");

        public static readonly Action<ILogger, Exception?> ApplyFailed =
            LoggerMessage.Define(
                LogLevel.Error,
                new EventId(1003, nameof(ApplyFailed)),
                "Failed to apply key repeat settings.");
    }
}