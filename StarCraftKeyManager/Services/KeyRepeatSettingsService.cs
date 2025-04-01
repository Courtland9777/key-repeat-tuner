using FluentValidation;
using Microsoft.Extensions.Options;
using StarCraftKeyManager.Configuration;
using StarCraftKeyManager.Interfaces;
using StarCraftKeyManager.SystemAdapters.Interfaces;

namespace StarCraftKeyManager.Services;

internal sealed class KeyRepeatSettingsService : IKeyRepeatSettingsService
{
    private readonly IKeyboardSettingsApplier _keyboardSettingsApplier;
    private readonly ILogger<KeyRepeatSettingsService> _logger;
    private readonly IValidator<AppSettings> _validator;
    private KeyRepeatSettings _settings;

    public KeyRepeatSettingsService(
        ILogger<KeyRepeatSettingsService> logger,
        IKeyboardSettingsApplier keyboardSettingsApplier,
        IOptionsMonitor<AppSettings> optionsMonitor,
        IValidator<AppSettings> validator)
    {
        _logger = logger;
        _keyboardSettingsApplier = keyboardSettingsApplier;
        _validator = validator;

        _settings = optionsMonitor.CurrentValue.KeyRepeat;

        optionsMonitor.OnChange(OnSettingsChanged);
    }

    public void UpdateRunningState(bool isRunning)
    {
        try
        {
            var mode = isRunning ? "FastMode" : "Default";
            var config = isRunning ? _settings.FastMode : _settings.Default;

            _logger.LogInformation("Applying key repeat settings: Mode={Mode}, Speed={Speed}, Delay={Delay}",
                mode, config.RepeatSpeed, config.RepeatDelay);

            _keyboardSettingsApplier.ApplyRepeatSettings(config.RepeatSpeed, config.RepeatDelay);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply key repeat settings for state: Running={IsRunning}", isRunning);
        }
    }

    private void OnSettingsChanged(AppSettings newSettings)
    {
        var result = _validator.Validate(newSettings);

        if (!result.IsValid)
        {
            _logger.LogWarning("Rejected invalid key repeat settings update at runtime.");
            foreach (var failure in result.Errors.Where(failure => failure.PropertyName.StartsWith("KeyRepeat")))
                _logger.LogWarning("KeyRepeat validation failed: {Property} - {Message}",
                    failure.PropertyName, failure.ErrorMessage);

            return;
        }

        _logger.LogInformation("Key repeat settings updated successfully at runtime.");
        _settings = newSettings.KeyRepeat;
    }
}