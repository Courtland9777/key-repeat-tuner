using KeyRepeatTuner.Configuration;
using KeyRepeatTuner.Interfaces;
using Microsoft.Extensions.Options;

namespace KeyRepeatTuner.Services;

internal sealed class KeyRepeatModeCoordinator : IKeyRepeatSettingsService, IAppSettingsChangeHandler
{
    private readonly IKeyRepeatApplier _applier;
    private readonly ILogger<KeyRepeatModeCoordinator> _logger;
    private readonly KeyRepeatModeResolver _resolver;
    private KeyRepeatSettings _settings;

    public KeyRepeatModeCoordinator(
        ILogger<KeyRepeatModeCoordinator> logger,
        IKeyRepeatApplier applier,
        IOptionsMonitor<AppSettings> options,
        KeyRepeatModeResolver resolver)
    {
        _logger = logger;
        _applier = applier;
        _resolver = resolver;
        _settings = options.CurrentValue.KeyRepeat;
    }

    public void OnSettingsChanged(AppSettings newSettings)
    {
        _settings = newSettings.KeyRepeat;
        _logger.LogInformation("KeyRepeat settings updated via runtime config.");
    }

    public void UpdateRunningState(bool isRunning)
    {
        var mode = KeyRepeatModeResolver.GetModeName(isRunning);
        var config = KeyRepeatModeResolver.GetTargetState(isRunning, _settings);

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