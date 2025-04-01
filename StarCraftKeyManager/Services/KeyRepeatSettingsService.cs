using FluentValidation;
using Microsoft.Extensions.Options;
using StarCraftKeyManager.Configuration;
using StarCraftKeyManager.Interfaces;
using StarCraftKeyManager.SystemAdapters.Interfaces;

namespace StarCraftKeyManager.Services;

internal sealed class KeyRepeatSettingsService : IKeyRepeatSettingsService, IDisposable
{
    private readonly IDisposable? _changeRegistration;
    private readonly IKeyboardSettingsApplier _keyboardSettingsApplier;
    private readonly ILogger<KeyRepeatSettingsService> _logger;
    private KeyRepeatSettings _settings;

    public KeyRepeatSettingsService(
        ILogger<KeyRepeatSettingsService> logger,
        IKeyboardSettingsApplier keyboardSettingsApplier,
        IOptionsMonitor<AppSettings> optionsMonitor,
        IValidator<AppSettings> validator)
    {
        _logger = logger;
        _keyboardSettingsApplier = keyboardSettingsApplier;
        _settings = optionsMonitor.CurrentValue.KeyRepeat;

        _changeRegistration = optionsMonitor.OnChange(newSettings =>
        {
            var result = validator.Validate(newSettings);

            if (!result.IsValid)
            {
                _logger.LogError("KeyRepeat validation failed. Ignoring new settings.");
                foreach (var error in result.Errors)
                    Log.ValidationError(_logger, error.PropertyName, error.ErrorMessage, null);

                return;
            }

            Log.ConfigSuccess(_logger, "Key repeat settings updated successfully at runtime.", null);
            _settings = newSettings.KeyRepeat;
        });
    }

    public void Dispose()
    {
        _changeRegistration?.Dispose();
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

        public static readonly Action<ILogger, string, Exception?> ConfigSuccess =
            LoggerMessage.Define<string>(
                LogLevel.Information,
                new EventId(1001, nameof(ConfigSuccess)),
                "{Message}");

        public static readonly Action<ILogger, string, string, Exception?> ValidationError =
            LoggerMessage.Define<string, string>(
                LogLevel.Error,
                new EventId(1002, nameof(ValidationError)),
                "{Property}: {Message}");

        public static readonly Action<ILogger, Exception?> ApplyFailed =
            LoggerMessage.Define(
                LogLevel.Error,
                new EventId(1003, nameof(ApplyFailed)),
                "Failed to apply key repeat settings.");
    }
}