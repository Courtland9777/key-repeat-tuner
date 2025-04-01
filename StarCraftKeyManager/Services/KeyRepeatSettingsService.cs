using FluentValidation;
using Microsoft.Extensions.Options;
using StarCraftKeyManager.Configuration;
using StarCraftKeyManager.Interfaces;
using StarCraftKeyManager.SystemAdapters.Interfaces;

namespace StarCraftKeyManager.Services;

internal sealed class KeyRepeatSettingsService : IKeyRepeatSettingsService, IDisposable
{
    private static readonly Action<ILogger, string, int, int, Exception?> LogApplySettings =
        LoggerMessage.Define<string, int, int>(
            LogLevel.Information,
            new EventId(1000, "ApplySettings"),
            "Applying key repeat settings: Mode={Mode}, Speed={Speed}, Delay={Delay}");

    private static readonly Action<ILogger, string, Exception?> LogSuccess =
        LoggerMessage.Define<string>(
            LogLevel.Information,
            new EventId(1001, "ConfigSuccess"),
            "{Message}");

    private static readonly Action<ILogger, string, string, Exception?> LogValidationError =
        LoggerMessage.Define<string, string>(
            LogLevel.Error,
            new EventId(1002, "ValidationError"),
            "{Property}: {Message}");

    private static readonly Action<ILogger, Exception?> LogApplyFailed =
        LoggerMessage.Define(
            LogLevel.Error,
            new EventId(1003, "ApplyFailed"),
            "Failed to apply key repeat settings.");

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
                    LogValidationError(_logger, error.PropertyName, error.ErrorMessage, null);

                return;
            }

            LogSuccess(_logger, "Key repeat settings updated successfully at runtime.", null);
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

            LogApplySettings(_logger, mode, config.RepeatSpeed, config.RepeatDelay, null);
            _keyboardSettingsApplier.ApplyRepeatSettings(config.RepeatSpeed, config.RepeatDelay);
        }
        catch (Exception ex)
        {
            LogApplyFailed(_logger, ex);
        }
    }
}