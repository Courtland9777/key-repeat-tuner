using KeyRepeatTuner.Configuration;

namespace KeyRepeatTuner.Core.Services;

public sealed class KeyRepeatModeResolver
{
    public static KeyRepeatState GetTargetState(bool isRunning, KeyRepeatSettings settings)
    {
        return isRunning ? settings.FastMode : settings.Default;
    }

    public static string GetModeName(bool isRunning)
    {
        return isRunning ? "FastMode" : "Default";
    }
}