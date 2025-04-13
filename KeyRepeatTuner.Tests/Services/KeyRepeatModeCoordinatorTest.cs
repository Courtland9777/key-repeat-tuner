using KeyRepeatTuner.Configuration;
using KeyRepeatTuner.Interfaces;
using KeyRepeatTuner.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace KeyRepeatTuner.Tests.Services;

public class KeyRepeatModeCoordinatorTest
{
    private readonly Mock<IKeyRepeatApplier> _mockApplier = new();
    private readonly Mock<ILogger<KeyRepeatModeCoordinator>> _mockLogger = new();

    private static KeyRepeatSettings CreateSettings()
    {
        return new KeyRepeatSettings
        {
            Default = new KeyRepeatState { RepeatSpeed = 10, RepeatDelay = 750 },
            FastMode = new KeyRepeatState { RepeatSpeed = 25, RepeatDelay = 500 }
        };
    }

    private static AppSettings CreateAppSettings()
    {
        return new AppSettings
        {
            ProcessNames = ["notepad", "starcraft"],
            KeyRepeat = CreateSettings()
        };
    }

    private KeyRepeatModeCoordinator CreateCoordinator(AppSettings? settingsOverride = null)
    {
        var mockOptions = new Mock<IOptionsMonitor<AppSettings>>();
        mockOptions.Setup(o => o.CurrentValue).Returns(settingsOverride ?? CreateAppSettings());

        return new KeyRepeatModeCoordinator(_mockLogger.Object, _mockApplier.Object, mockOptions.Object,
            new KeyRepeatModeResolver());
    }

    [Fact]
    public void UpdateRunningState_True_ShouldApplyFastMode()
    {
        var coordinator = CreateCoordinator();
        coordinator.UpdateRunningState(true);

        _mockApplier.Verify(a => a.Apply(It.Is<KeyRepeatState>(
            s => s.RepeatSpeed == 25 && s.RepeatDelay == 500)), Times.Once);
    }

    [Fact]
    public void UpdateRunningState_False_ShouldApplyDefault()
    {
        var coordinator = CreateCoordinator();
        coordinator.UpdateRunningState(false);

        _mockApplier.Verify(a => a.Apply(It.Is<KeyRepeatState>(
            s => s.RepeatSpeed == 10 && s.RepeatDelay == 750)), Times.Once);
    }

    [Fact]
    public void OnSettingsChanged_ShouldUpdateInternalState_AndUseNewValues()
    {
        var coordinator = CreateCoordinator();

        var updatedSettings = new AppSettings
        {
            ProcessNames = ["quake"],
            KeyRepeat = new KeyRepeatSettings
            {
                Default = new KeyRepeatState { RepeatSpeed = 15, RepeatDelay = 1000 },
                FastMode = new KeyRepeatState { RepeatSpeed = 5, RepeatDelay = 250 }
            }
        };

        coordinator.OnSettingsChanged(updatedSettings);
        coordinator.UpdateRunningState(true);

        _mockApplier.Verify(a => a.Apply(It.Is<KeyRepeatState>(
            s => s.RepeatSpeed == 5 && s.RepeatDelay == 250)), Times.Once);
    }

    [Fact]
    public void UpdateRunningState_ShouldNotThrow_WhenApplierFails()
    {
        _mockApplier
            .Setup(a => a.Apply(It.IsAny<KeyRepeatState>()))
            .Throws<InvalidOperationException>();

        var coordinator = CreateCoordinator();

        var exception = Record.Exception(() => coordinator.UpdateRunningState(true));

        Assert.Null(exception);
    }
}