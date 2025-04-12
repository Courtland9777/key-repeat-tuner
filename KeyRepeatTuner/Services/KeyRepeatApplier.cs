using KeyRepeatTuner.Configuration;
using KeyRepeatTuner.Interfaces;
using KeyRepeatTuner.SystemAdapters.Interfaces;

namespace KeyRepeatTuner.Services;

internal sealed class KeyRepeatApplier : IKeyRepeatApplier
{
    private readonly ILogger<KeyRepeatApplier> _logger;
    private readonly IKeyboardSettingsApplier _nativeApplier;

    public KeyRepeatApplier(
        ILogger<KeyRepeatApplier> logger,
        IKeyboardSettingsApplier nativeApplier)
    {
        _logger = logger;
        _nativeApplier = nativeApplier;
    }

    public void Apply(KeyRepeatState state)
    {
        try
        {
            _logger.LogInformation("Applying keyboard repeat settings: Speed={Speed}, Delay={Delay}",
                state.RepeatSpeed, state.RepeatDelay);

            _nativeApplier.ApplyRepeatSettings(state.RepeatSpeed, state.RepeatDelay);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply keyboard repeat settings.");
        }
    }
}