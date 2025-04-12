using KeyRepeatTuner.Configuration;
using KeyRepeatTuner.Interfaces;
using Microsoft.Extensions.Options;

namespace KeyRepeatTuner.Services;

internal sealed class KeyRepeatModeCoordinator : IKeyRepeatSettingsService, IAppSettingsChangeHandler
{
    private readonly IKeyRepeatApplier _applier;
    private readonly ILogger<KeyRepeatModeCoordinator> _logger;
    private KeyRepeatSettings _settings;

    public KeyRepeatModeCoordinator(
        ILogger<KeyRepeatModeCoordinator> logger,
        IKeyRepeatApplier applier,
        IOptionsMonitor<AppSettings> options)
    {
        _logger = logger;
        _applier = applier;
        _settings = options.CurrentValue.KeyRepeat;
    }

    public void OnSettingsChanged(AppSettings newSettings)
    {
        _settings = newSettings.KeyRepeat;
        _logger.LogInformation("KeyRepeat settings updated via runtime config.");
    }

    public void UpdateRunningState(bool isRunning)
    {
        var mode = isRunning ? "FastMode" : "Default";
        var config = isRunning ? _settings.FastMode : _settings.Default;

        _logger.LogInformation("Changing state → Mode={Mode}, Speed={Speed}, Delay={Delay}",
            mode, config.RepeatSpeed, config.RepeatDelay);

        try
        {
            _applier.Apply(config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply key repeat settings for {Mode} mode.", mode);
        }
    }
}