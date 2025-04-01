using Microsoft.Extensions.Logging;
using Moq;
using StarCraftKeyManager.Configuration;
using StarCraftKeyManager.Tests.TestUtilities.Extensions;
using StarCraftKeyManager.Tests.TestUtilities.Stubs;
using Xunit;

namespace StarCraftKeyManager.Tests.Configuration;

public class AppSettingsValidatorTests
{
    private readonly Mock<ILogger<AppSettingsValidator>> _mockLogger;
    private readonly AppSettingsValidator _validator;

    public AppSettingsValidatorTests()
    {
        _mockLogger = new Mock<ILogger<AppSettingsValidator>>();
        _validator = new AppSettingsValidator(_mockLogger.Object);
    }

    [Fact]
    public void ValidConfiguration_ShouldPassValidation()
    {
        var validSettings = new AppSettings
        {
            ProcessName = "starcraft",
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
    public void InvalidProcessNameFormat_ShouldLogAndFailValidation()
    {
        var mockLogger = new Mock<ILogger<AppSettingsValidator>>();
        var validator = new AppSettingsValidator(mockLogger.Object);

        var settings = new AppSettings
        {
            ProcessName = string.Empty,
            KeyRepeat = new KeyRepeatSettings
            {
                Default = new KeyRepeatState { RepeatSpeed = 31, RepeatDelay = 1000 },
                FastMode = new KeyRepeatState { RepeatSpeed = 20, RepeatDelay = 500 }
            }
        };

        var result = validator.Validate(settings);

        Assert.False(result.IsValid);

        Assert.Contains(result.Errors,
            e => e.PropertyName == "ProcessName" &&
                 e.ErrorMessage.Contains("valid process name", StringComparison.OrdinalIgnoreCase));

        mockLogger.Verify(logger => logger.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            MoqLogExtensions.MatchLogState("Validation failed for ProcessName"),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.AtLeastOnce);
    }


    [Fact]
    public void ChangingAppSettings_ShouldTriggerValidation()
    {
        // Arrange
        var initialSettings = new AppSettings
        {
            ProcessName = "starcraft",
            KeyRepeat = new KeyRepeatSettings
            {
                Default = new KeyRepeatState { RepeatSpeed = 31, RepeatDelay = 1000 },
                FastMode = new KeyRepeatState { RepeatSpeed = 20, RepeatDelay = 500 }
            }
        };

        var updatedSettings = new AppSettings
        {
            ProcessName = string.Empty,
            KeyRepeat = new KeyRepeatSettings
            {
                Default = new KeyRepeatState { RepeatSpeed = 50, RepeatDelay = 2000 },
                FastMode = new KeyRepeatState { RepeatSpeed = -1, RepeatDelay = 100 }
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

        Assert.Contains(updatedResult.Errors, e => e.PropertyName == "ProcessName");
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
            ProcessName = "starcraft",
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
            ProcessName = "starcraft",
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
            ProcessName = "starcraft",
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
            ProcessName = "starcraft",
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
}