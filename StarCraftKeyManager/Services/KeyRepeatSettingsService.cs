using Microsoft.Extensions.Options;
using StarCraftKeyManager.Configuration;
using StarCraftKeyManager.Interfaces;
using StarCraftKeyManager.SystemAdapters.Interfaces;

namespace StarCraftKeyManager.Services;

internal sealed class KeyRepeatSettingsService : IKeyRepeatSettingsService
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

        optionsMonitor.OnChange(newSettings => { _settings = newSettings.KeyRepeat; });
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
}