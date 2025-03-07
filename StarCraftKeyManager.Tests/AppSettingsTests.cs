using StarCraftKeyManager.Models;
using StarCraftKeyManager.Validators;

namespace StarCraftKeyManager.Tests;

public class AppSettingsTests
{
    private readonly AppSettingsValidator _validator = new();

    [Fact]
    public void ValidConfiguration_ShouldPassValidation()
    {
        var validSettings = new AppSettings
        {
            ProcessMonitor = new ProcessMonitorSettings { ProcessName = "starcraft.exe" },
            KeyRepeat = new KeyRepeatSettings
            {
                Default = new KeyRepeatState { RepeatSpeed = 31, RepeatDelay = 1000 },
                FastMode = new KeyRepeatState { RepeatSpeed = 20, RepeatDelay = 500 }
            }
        };

        var result = _validator.Validate(validSettings);
        Assert.True(result.IsValid, "Expected configuration to be valid.");
    }

    [Theory]
    [InlineData(-1, 1000)]
    [InlineData(32, 1000)]
    [InlineData(20, 2000)]
    public void InvalidConfiguration_ShouldFailValidation(int repeatSpeed, int repeatDelay)
    {
        var invalidSettings = new AppSettings
        {
            ProcessMonitor = new ProcessMonitorSettings { ProcessName = "starcraft.exe" },
            KeyRepeat = new KeyRepeatSettings
            {
                Default = new KeyRepeatState { RepeatSpeed = repeatSpeed, RepeatDelay = repeatDelay },
                FastMode = new KeyRepeatState { RepeatSpeed = 20, RepeatDelay = 500 }
            }
        };

        var result = _validator.Validate(invalidSettings);
        Assert.False(result.IsValid, "Expected configuration to be invalid.");
    }
}