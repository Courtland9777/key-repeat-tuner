using KeyRepeatTuner.Configuration;
using KeyRepeatTuner.Configuration.Validation;
using KeyRepeatTuner.Configuration.ValueObjects;
using KeyRepeatTuner.Tests.TestUtilities.Stubs;
using Xunit;

namespace KeyRepeatTuner.Tests.Configuration;

public class AppSettingsValidatorTests
{
    private readonly AppSettingsValidator _validator = new();

    [Fact]
    public void ValidConfiguration_ShouldPassValidation()
    {
        var validSettings = new AppSettings
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

        var result = _validator.Validate(validSettings);

        Assert.True(result.IsValid, "Expected configuration to be valid.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_ShouldThrow_WhenInputIsNullOrWhitespace(string? input)
    {
        Assert.Throws<ArgumentException>(() => _ = new ProcessName(input!));
    }

    [Theory]
    [InlineData("cmd", "cmd")]
    [InlineData("notepad.exe", "notepad")]
    [InlineData("  starcraft.exe  ", "starcraft")]
    public void Constructor_ShouldNormalizeValidInput(string input, string expected)
    {
        var name = new ProcessName(input);

        Assert.Equal(expected, name.Value);
        Assert.Equal($"{expected}.exe", name.WithExe());
    }

    [Fact]
    public void ChangingAppSettings_ShouldTriggerValidation()
    {
        // Arrange
        var initialSettings = new AppSettings
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

        var updatedSettings = new AppSettings
        {
            ProcessNames =
            [
                new ProcessName("starcraft"),
                new ProcessName("notepad")
            ],
            KeyRepeat = new KeyRepeatSettings
            {
                Default = new KeyRepeatState { RepeatSpeed = 40, RepeatDelay = 2000 },
                FastMode = new KeyRepeatState { RepeatSpeed = 33, RepeatDelay = -1 }
            }
        };

        var optionsMonitor = new TestOptionsMonitor<AppSettings>(initialSettings);
        var validator = new AppSettingsValidator();

        var initialResult = validator.Validate(optionsMonitor.CurrentValue);
        Assert.True(initialResult.IsValid);

        // Act
        optionsMonitor.TriggerChange(updatedSettings);
        var updatedResult = validator.Validate(optionsMonitor.CurrentValue);

        // Assert
        Assert.False(updatedResult.IsValid);
        Assert.Contains(updatedResult.Errors, e => e.PropertyName == "KeyRepeat.Default.RepeatSpeed");
        Assert.Contains(updatedResult.Errors, e => e.PropertyName == "KeyRepeat.Default.RepeatDelay");
        Assert.Contains(updatedResult.Errors, e => e.PropertyName == "KeyRepeat.FastMode.RepeatSpeed");
        Assert.Contains(updatedResult.Errors, e => e.PropertyName == "KeyRepeat.FastMode.RepeatDelay");
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
            ProcessNames = [new ProcessName("starcraft"), new ProcessName("notepad")],
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
    public void MissingKeyRepeatSettings_ShouldFailValidation()
    {
        var invalidSettings = new AppSettings
        {
            ProcessNames =
            [
                new ProcessName("starcraft"),
                new ProcessName("notepad")
            ],
            KeyRepeat = new KeyRepeatSettings
            {
                Default = null!,
                FastMode = null!
            }
        };

        var result = _validator.Validate(invalidSettings);

        Assert.False(result.IsValid, "Expected configuration to be invalid due to missing KeyRepeat settings.");
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("Default key repeat settings must be provided."));
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("FastMode key repeat settings must be provided."));
    }


    [Fact]
    public void MissingDefaultKeyRepeatState_ShouldFailValidation()
    {
        var invalidSettings = new AppSettings
        {
            ProcessNames = [new ProcessName("starcraft"), new ProcessName("notepad")],
            KeyRepeat = new KeyRepeatSettings
            {
                Default = null!, // trigger validation error
                FastMode = new KeyRepeatState { RepeatSpeed = 20, RepeatDelay = 500 }
            }
        };

        var result = _validator.Validate(invalidSettings);

        Assert.False(result.IsValid,
            "Expected configuration to be invalid due to missing Default key repeat settings.");
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("Default key repeat settings must be provided."));
    }


    [Fact]
    public void MissingFastModeKeyRepeatState_ShouldFailValidation()
    {
        var invalidSettings = new AppSettings
        {
            ProcessNames = [new ProcessName("starcraft"), new ProcessName("notepad")],
            KeyRepeat = new KeyRepeatSettings
            {
                Default = new KeyRepeatState { RepeatSpeed = 31, RepeatDelay = 1000 },
                FastMode = null!
            }
        };

        var result = _validator.Validate(invalidSettings);

        Assert.False(result.IsValid, "Expected configuration to be invalid due to missing FastMode.");
        Assert.Contains(result.Errors, e =>
            e.PropertyName == "KeyRepeat.FastMode" &&
            e.ErrorMessage == "FastMode key repeat settings must be provided.");
    }

    [Fact]
    public void EmptyProcessNames_ShouldFailValidation()
    {
        var settings = new AppSettings
        {
            ProcessNames = [],
            KeyRepeat = new KeyRepeatSettings
            {
                Default = new KeyRepeatState { RepeatSpeed = 20, RepeatDelay = 500 },
                FastMode = new KeyRepeatState { RepeatSpeed = 15, RepeatDelay = 300 }
            }
        };

        var validator = new AppSettingsValidator();

        var result = validator.Validate(settings);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("At least one process name must be specified"));
    }
}