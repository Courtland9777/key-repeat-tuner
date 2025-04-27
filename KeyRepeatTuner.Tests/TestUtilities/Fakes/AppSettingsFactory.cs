using KeyRepeatTuner.Configuration;
using KeyRepeatTuner.Configuration.ValueObjects;

namespace KeyRepeatTuner.Tests.TestUtilities.Fakes;

internal static class AppSettingsFactory
{
    public static AppSettings CreateDefault()
    {
        return new AppSettings
        {
            ProcessNames =
            [
                new ProcessName("starcraft"),
                new ProcessName("notepad")
            ],
            KeyRepeat = new KeyRepeatSettings
            {
                Default = new KeyRepeatState { RepeatSpeed = 31, RepeatDelay = 1000 },
                FastMode = new KeyRepeatState { RepeatSpeed = 20, RepeatDelay = 500 }
            }
        };
    }
}