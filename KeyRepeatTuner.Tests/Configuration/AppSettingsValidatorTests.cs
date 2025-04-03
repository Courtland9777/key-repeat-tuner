using System.Text.Json;
using KeyRepeatTuner.Configuration;
using KeyRepeatTuner.Configuration.Converters;
using KeyRepeatTuner.Configuration.Validation;
using KeyRepeatTuner.Configuration.ValueObjects;
using KeyRepeatTuner.Tests.TestUtilities.Stubs;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace KeyRepeatTuner.Tests.Configuration;

public class AppSettingsValidatorTests
{
    private readonly Mock<ILogger<AppSettingsValidator>> _mockLogger;
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly AppSettingsValidator _validator;

    public AppSettingsValidatorTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _mockLogger = new Mock<ILogger<AppSettingsValidator>>();
        _validator = new AppSettingsValidator();
    }

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

    [Fact]
    public void NullProcessName_ShouldFailValidation()
    {
        // Arrange
        var settings = new AppSettings
        {
            ProcessNames = [null!],
            KeyRepeat = new KeyRepeatSettings
            {
                Default = new KeyRepeatState { RepeatSpeed = 20, RepeatDelay = 500 },
                FastMode = new KeyRepeatState { RepeatSpeed = 30, RepeatDelay = 300 }
            }
        };

        var validator = new AppSettingsValidator();

        // Act
        var result = validator.Validate(settings);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("cannot be null"));
    }


    [Fact]
    public void InvalidProcessName_ShouldThrowDuringDeserialization()
    {
        // Arrange
        var json = """
                   {
                       "ProcessNames": [ "Invalid Name With Spaces" ],
                       "KeyRepeat": {
                           "Default": { "RepeatSpeed": 20, "RepeatDelay": 1000 },
                           "FastMode": { "RepeatSpeed": 31, "RepeatDelay": 500 }
                       }
                   }
                   """;

        // Act & Assert
        var ex = Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<AppSettings>(json, new JsonSerializerOptions
            {
                Converters = { new ProcessNameListJsonConverter() }
            }));

        Assert.Contains("Invalid process name format", ex.InnerException!.Message);
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
}