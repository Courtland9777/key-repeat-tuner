using KeyRepeatTuner.Configuration;
using KeyRepeatTuner.Interfaces;
using KeyRepeatTuner.SystemAdapters.Interfaces;
using Microsoft.Extensions.Options;

namespace KeyRepeatTuner.Services;

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

            _logger.LogInformation("Applying key repeat settings: Mode={Mode}, Speed={Speed}, Delay={Delay}",
                mode, config.RepeatSpeed, config.RepeatDelay);

            _keyboardSettingsApplier.ApplyRepeatSettings(config.RepeatSpeed, config.RepeatDelay);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply key repeat settings.");
        }
    }
}