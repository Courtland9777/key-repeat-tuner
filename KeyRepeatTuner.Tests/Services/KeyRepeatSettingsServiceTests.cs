using KeyRepeatTuner.Configuration;
using KeyRepeatTuner.Configuration.ValueObjects;
using KeyRepeatTuner.Services;
using KeyRepeatTuner.SystemAdapters.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace KeyRepeatTuner.Tests.Services;

public class KeyRepeatSettingsServiceTests
{
    private readonly Mock<IKeyboardSettingsApplier> _mockKeyboardApplier = new();
    private readonly Mock<ILogger<KeyRepeatSettingsService>> _mockLogger = new();

    private KeyRepeatSettingsService CreateService(AppSettings settings)
    {
        var mockOptions = new Mock<IOptionsMonitor<AppSettings>>();
        mockOptions.Setup(o => o.CurrentValue).Returns(settings);
        return new KeyRepeatSettingsService(
            _mockLogger.Object,
            _mockKeyboardApplier.Object,
            mockOptions.Object
        );
    }

    [Fact]
    public void UpdateRunningState_ShouldApplyFastMode_WhenRunning()
    {
        // Arrange
        var settings = CreateAppSettings(
            new KeyRepeatState { RepeatSpeed = 10, RepeatDelay = 750 },
            new KeyRepeatState { RepeatSpeed = 20, RepeatDelay = 500 },
            "sc", "notepad"
        );
        var service = CreateService(settings);
        // Act
        service.UpdateRunningState(true);
        // Assert
        _mockKeyboardApplier.Verify(a => a.ApplyRepeatSettings(20, 500), Times.Once);
    }

    [Fact]
    public void UpdateRunningState_ShouldApplyDefaultMode_WhenNotRunning()
    {
        // Arrange
        var settings = CreateAppSettings(
            new KeyRepeatState { RepeatSpeed = 10, RepeatDelay = 750 },
            new KeyRepeatState { RepeatSpeed = 20, RepeatDelay = 500 },
            "sc"
        );
        var service = CreateService(settings);
        // Act
        service.UpdateRunningState(false);
        // Assert
        _mockKeyboardApplier.Verify(a => a.ApplyRepeatSettings(10, 750), Times.Once);
    }

    [Fact]
    public void OnSettingsChanged_ShouldUpdateInternalState()
    {
        // Arrange
        var initialSettings = CreateAppSettings(
            new KeyRepeatState { RepeatSpeed = 10, RepeatDelay = 750 },
            new KeyRepeatState { RepeatSpeed = 20, RepeatDelay = 500 },
            "sc"
        );
        var updatedSettings = CreateAppSettings(
            new KeyRepeatState { RepeatSpeed = 5, RepeatDelay = 250 },
            new KeyRepeatState { RepeatSpeed = 15, RepeatDelay = 400 },
            "sc"
        );
        var service = CreateService(initialSettings);
        // Act
        service.OnSettingsChanged(updatedSettings);
        service.UpdateRunningState(false);
        // Assert
        _mockKeyboardApplier.Verify(a => a.ApplyRepeatSettings(5, 250), Times.Once);
    }

    [Fact]
    public void UpdateRunningState_ShouldNotThrow_WhenApplierFails()
    {
        // Arrange
        _mockKeyboardApplier
            .Setup(a => a.ApplyRepeatSettings(It.IsAny<int>(), It.IsAny<int>()))
            .Throws<InvalidOperationException>();
        var settings = CreateAppSettings(
            new KeyRepeatState { RepeatSpeed = 1, RepeatDelay = 500 },
            new KeyRepeatState { RepeatSpeed = 1, RepeatDelay = 500 },
            "sc"
        );
        var service = CreateService(settings);
        // Act
        var exception = Record.Exception(() => service.UpdateRunningState(true));
        // Assert
        Assert.Null(exception);
    }

    private static AppSettings CreateAppSettings(
        KeyRepeatState defaultState,
        KeyRepeatState fastModeState,
        params string[] processNames
    )
    {
        return new AppSettings
        {
            ProcessNames = [.. processNames.Select(name => new ProcessName(name))],
            KeyRepeat = new KeyRepeatSettings
            {
                Default = defaultState,
                FastMode = fastModeState
            }
        };
    }
}