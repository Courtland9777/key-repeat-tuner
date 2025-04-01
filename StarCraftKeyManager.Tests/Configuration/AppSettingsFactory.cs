using StarCraftKeyManager.Configuration;

namespace StarCraftKeyManager.Tests.Configuration;

public static class AppSettingsFactory
{
    public static AppSettings CreateDefault()
    {
        return new AppSettings
        {
            ProcessName = "starcraft",
            KeyRepeat = new KeyRepeatSettings
            {
                Default = new KeyRepeatState { RepeatSpeed = 31, RepeatDelay = 1000 },
                FastMode = new KeyRepeatState { RepeatSpeed = 20, RepeatDelay = 500 }
            }
        };
    }
}