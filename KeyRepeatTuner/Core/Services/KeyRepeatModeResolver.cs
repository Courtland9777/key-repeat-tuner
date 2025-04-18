using KeyRepeatTuner.Configuration;
using KeyRepeatTuner.Core.Interfaces;

namespace KeyRepeatTuner.Core.Services;

public sealed class KeyRepeatModeResolver : IKeyRepeatModeResolver
{
    public KeyRepeatState GetTargetState(bool isRunning, KeyRepeatSettings settings)
    {
        return isRunning ? settings.FastMode : settings.Default;
    }

    public string GetModeName(bool isRunning)
    {
        return isRunning ? "FastMode" : "Default";
    }
}