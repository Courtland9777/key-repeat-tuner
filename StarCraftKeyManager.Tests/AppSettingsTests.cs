using Microsoft.Extensions.Options;
using Moq;
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

    [Fact]
    public void EmptyProcessMonitorName_ShouldFailValidation()
    {
        var invalidSettings = new AppSettings
        {
            ProcessMonitor = new ProcessMonitorSettings { ProcessName = "" }, // Invalid
            KeyRepeat = new KeyRepeatSettings
            {
                Default = new KeyRepeatState { RepeatSpeed = 31, RepeatDelay = 1000 },
                FastMode = new KeyRepeatState { RepeatSpeed = 20, RepeatDelay = 500 }
            }
        };

        var result = _validator.Validate(invalidSettings);

        Assert.False(result.IsValid, "Expected configuration to be invalid due to empty ProcessMonitor.ProcessName.");
        Assert.Contains(result.Errors,
            e => e.PropertyName == "ProcessMonitor.ProcessName" && e.ErrorMessage.Contains("required"));
    }

    [Fact]
    public void ChangingAppSettings_ShouldTriggerValidation()
    {
        var options = new Mock<IOptionsMonitor<AppSettings>>();
        var settings = new AppSettings
        {
            ProcessMonitor = new ProcessMonitorSettings { ProcessName = "starcraft.exe" },
            KeyRepeat = new KeyRepeatSettings
            {
                Default = new KeyRepeatState { RepeatSpeed = 31, RepeatDelay = 1000 },
                FastMode = new KeyRepeatState { RepeatSpeed = 20, RepeatDelay = 500 }
            }
        };

        options.Setup(o => o.CurrentValue).Returns(settings);

        var eventTriggered = false;
        options.Setup(o => o.OnChange(It.IsAny<Action<AppSettings>>()))
            .Callback<Action<AppSettings>>(callback =>
            {
                eventTriggered = true;
                callback(new AppSettings
                {
                    ProcessMonitor = new ProcessMonitorSettings
                    {
                        ProcessName = ""
                    },
                    KeyRepeat = new KeyRepeatSettings
                    {
                        Default = new KeyRepeatState { RepeatSpeed = 31, RepeatDelay = 1000 },
                        FastMode = new KeyRepeatState { RepeatSpeed = 20, RepeatDelay = 500 }
                    }
                });
            });

        Assert.True(eventTriggered, "Expected OnChange event to trigger when settings change.");
    }

    [Theory]
    [InlineData(-1, 1000)] // RepeatSpeed too low
    [InlineData(32, 1000)] // RepeatSpeed too high
    [InlineData(20, 2000)] // RepeatDelay too high
    [InlineData(20, 200)] // RepeatDelay too low
    public void InvalidKeyRepeatSettings_ShouldFailValidation(int repeatSpeed, int repeatDelay)
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
        Assert.Contains(result.Errors,
            e => e.PropertyName.Contains("RepeatSpeed") || e.PropertyName.Contains("RepeatDelay"));
    }

    [Fact]
    public void MissingProcessMonitor_ShouldFailValidation()
    {
        var invalidSettings = new AppSettings
        {
            KeyRepeat = new KeyRepeatSettings
            {
                Default = new KeyRepeatState { RepeatSpeed = 31, RepeatDelay = 1000 },
                FastMode = new KeyRepeatState { RepeatSpeed = 20, RepeatDelay = 500 }
            }
        };

        var result = _validator.Validate(invalidSettings);

        Assert.False(result.IsValid, "Expected configuration to be invalid due to missing ProcessMonitor.");
        Assert.Contains(result.Errors, e => e.PropertyName == "ProcessMonitor" && e.ErrorMessage.Contains("required"));
    }

    [Fact]
    public void MissingKeyRepeatSettings_ShouldFailValidation()
    {
        var invalidSettings = new AppSettings
        {
            ProcessMonitor = new ProcessMonitorSettings { ProcessName = "starcraft.exe" },
            KeyRepeat = new KeyRepeatSettings
            {
                Default = new KeyRepeatState { RepeatSpeed = 31, RepeatDelay = 1000 }, // ✅ Set Default
                FastMode = new KeyRepeatState { RepeatSpeed = 20, RepeatDelay = 500 }
            }
        };

        var result = _validator.Validate(invalidSettings);

        Assert.False(result.IsValid, "Expected configuration to be invalid due to missing KeyRepeat settings.");
        Assert.Contains(result.Errors, e => e.PropertyName == "KeyRepeat" && e.ErrorMessage.Contains("required"));
    }

    [Fact]
    public void MissingDefaultKeyRepeatState_ShouldFailValidation()
    {
        var invalidSettings = new AppSettings
        {
            ProcessMonitor = new ProcessMonitorSettings { ProcessName = "starcraft.exe" },
            KeyRepeat = new KeyRepeatSettings
            {
                Default = null!,
                FastMode = new KeyRepeatState { RepeatSpeed = 20, RepeatDelay = 500 }
            }
        };

        var result = _validator.Validate(invalidSettings);

        Assert.False(result.IsValid, "Expected configuration to be invalid due to missing Default KeyRepeat state.");
        Assert.Contains(result.Errors, e => e.PropertyName.Contains("Default") && e.ErrorMessage.Contains("required"));
    }

    [Fact]
    public void MissingFastModeKeyRepeatState_ShouldFailValidation()
    {
        var invalidSettings = new AppSettings
        {
            ProcessMonitor = new ProcessMonitorSettings { ProcessName = "starcraft.exe" },
            KeyRepeat = new KeyRepeatSettings
            {
                Default = new KeyRepeatState { RepeatSpeed = 31, RepeatDelay = 1000 },
                FastMode = null!
            }
        };

        var result = _validator.Validate(invalidSettings);

        Assert.False(result.IsValid, "Expected configuration to be invalid due to missing FastMode KeyRepeat state.");
        Assert.Contains(result.Errors, e => e.PropertyName.Contains("FastMode") && e.ErrorMessage.Contains("required"));
    }
}