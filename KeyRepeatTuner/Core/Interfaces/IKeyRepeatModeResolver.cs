using KeyRepeatTuner.Configuration;

namespace KeyRepeatTuner.Core.Interfaces;

public interface IKeyRepeatModeResolver
{
    KeyRepeatState GetTargetState(bool isRunning, KeyRepeatSettings settings);
    string GetModeName(bool isRunning);
}