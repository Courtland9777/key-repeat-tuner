using KeyRepeatTuner.Configuration;
using KeyRepeatTuner.Configuration.ValueObjects;
using KeyRepeatTuner.Core.Interfaces;
using KeyRepeatTuner.Core.Services;
using KeyRepeatTuner.Tests.TestUtilities.Extensions;
using KeyRepeatTuner.Tests.TestUtilities.Stubs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace KeyRepeatTuner.Tests.Core.Services;

public class KeyRepeatModeCoordinatorTests
{
    private readonly Mock<IKeyRepeatApplier> _applierMock = new();
    private readonly Mock<ILogger<KeyRepeatModeCoordinator>> _loggerMock = new();

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
            ProcessNames =
                [BuilderExtensions.CreateProcessName("notepad"), BuilderExtensions.CreateProcessName("starcraft")],
            KeyRepeat = CreateSettings()
        };
    }

    private KeyRepeatModeCoordinator CreateCoordinatorWithRealResolver(AppSettings? overrideSettings = null)
    {
        var options = new Mock<IOptionsMonitor<AppSettings>>();
        options.Setup(o => o.CurrentValue).Returns(overrideSettings ?? CreateAppSettings());

        return new KeyRepeatModeCoordinator(_loggerMock.Object, _applierMock.Object, options.Object,
            new KeyRepeatModeResolver());
    }

    private KeyRepeatModeCoordinator CreateCoordinatorWithMockedResolver(KeyRepeatSettings settings,
        Mock<IKeyRepeatModeResolver> resolverMock)
    {
        var options = new TestOptionsMonitor<AppSettings>(new AppSettings
        {
            ProcessNames = [new ProcessName("dummy")],
            KeyRepeat = settings
        });

        return new KeyRepeatModeCoordinator(_loggerMock.Object, _applierMock.Object, options, resolverMock.Object);
    }

    [Fact]
    public void UpdateRunningState_True_ShouldApplyFastMode()
    {
        var coordinator = CreateCoordinatorWithRealResolver();
        coordinator.UpdateRunningState(true);

        _applierMock.Verify(a => a.Apply(It.Is<KeyRepeatState>(s => s.RepeatSpeed == 25 && s.RepeatDelay == 500)),
            Times.Once);
    }

    [Fact]
    public void UpdateRunningState_False_ShouldApplyDefault()
    {
        var coordinator = CreateCoordinatorWithRealResolver();
        coordinator.UpdateRunningState(false);

        _applierMock.Verify(a => a.Apply(It.Is<KeyRepeatState>(s => s.RepeatSpeed == 10 && s.RepeatDelay == 750)),
            Times.Once);
    }

    [Fact]
    public void OnSettingsChanged_ShouldUpdateInternalState_AndUseNewValues()
    {
        var coordinator = CreateCoordinatorWithRealResolver();

        var updatedSettings = new AppSettings
        {
            ProcessNames = [BuilderExtensions.CreateProcessName("quake")],
            KeyRepeat = new KeyRepeatSettings
            {
                Default = new KeyRepeatState { RepeatSpeed = 15, RepeatDelay = 1000 },
                FastMode = new KeyRepeatState { RepeatSpeed = 5, RepeatDelay = 250 }
            }
        };

        coordinator.OnSettingsChanged(updatedSettings);
        coordinator.UpdateRunningState(true);

        _applierMock.Verify(a => a.Apply(It.Is<KeyRepeatState>(s => s.RepeatSpeed == 5 && s.RepeatDelay == 250)),
            Times.Once);
    }

    [Fact]
    public void UpdateRunningState_ShouldNotThrow_WhenApplierFails()
    {
        _applierMock.Setup(a => a.Apply(It.IsAny<KeyRepeatState>())).Throws<InvalidOperationException>();

        var coordinator = CreateCoordinatorWithRealResolver();

        var exception = Record.Exception(() => coordinator.UpdateRunningState(true));

        Assert.Null(exception);
    }

    [Fact]
    public void UpdateRunningState_ShouldLogAppliedFastMode()
    {
        var coordinator = CreateCoordinatorWithRealResolver();

        coordinator.UpdateRunningState(true);

        _loggerMock.VerifyLogContains(LogLevel.Information, "Changing state → Mode=FastMode");
    }

    [Fact]
    public void UpdateRunningState_ShouldLogAppliedDefaultMode()
    {
        var coordinator = CreateCoordinatorWithRealResolver();

        coordinator.UpdateRunningState(false);

        _loggerMock.VerifyLogContains(LogLevel.Information, "Changing state → Mode=Default");
    }

    [Fact]
    public void UpdateRunningState_WhenApplyThrows_ShouldLogWarning()
    {
        _applierMock.Setup(a => a.Apply(It.IsAny<KeyRepeatState>()))
            .Throws(new InvalidOperationException("Keyboard API error"));

        var coordinator = CreateCoordinatorWithRealResolver();

        coordinator.UpdateRunningState(true);

        _loggerMock.VerifyLogContains(LogLevel.Error, "Failed to apply key repeat settings");
    }

    [Theory]
    [InlineData(true, "FastMode", 10, 500)]
    [InlineData(false, "Default", 20, 1000)]
    public void UpdateRunningState_WithMockedResolver_ShouldApplyCorrectSettings(bool isRunning, string expectedMode,
        int expectedSpeed, int expectedDelay)
    {
        var settings = new KeyRepeatSettings
        {
            Default = new KeyRepeatState { RepeatSpeed = 20, RepeatDelay = 1000 },
            FastMode = new KeyRepeatState { RepeatSpeed = 10, RepeatDelay = 500 }
        };

        var resolverMock = new Mock<IKeyRepeatModeResolver>();
        resolverMock.Setup(r => r.GetTargetState(isRunning, settings)).Returns(new KeyRepeatState
            { RepeatSpeed = expectedSpeed, RepeatDelay = expectedDelay });
        resolverMock.Setup(r => r.GetModeName(isRunning)).Returns(expectedMode);

        var coordinator = CreateCoordinatorWithMockedResolver(settings, resolverMock);

        coordinator.UpdateRunningState(isRunning);

        _applierMock.Verify(
            x => x.Apply(It.Is<KeyRepeatState>(s => s.RepeatSpeed == expectedSpeed && s.RepeatDelay == expectedDelay)),
            Times.Once);
    }

    [Fact]
    public void OnSettingsChanged_WithMockedResolver_ShouldUpdateSettings()
    {
        var initial = new KeyRepeatSettings
        {
            Default = new KeyRepeatState { RepeatSpeed = 20, RepeatDelay = 1000 },
            FastMode = new KeyRepeatState { RepeatSpeed = 10, RepeatDelay = 500 }
        };

        var updated = new KeyRepeatSettings
        {
            Default = new KeyRepeatState { RepeatSpeed = 5, RepeatDelay = 300 },
            FastMode = new KeyRepeatState { RepeatSpeed = 31, RepeatDelay = 250 }
        };

        var newSettings = new AppSettings
        {
            ProcessNames = [new ProcessName("newprocess")],
            KeyRepeat = updated
        };

        var resolverMock = new Mock<IKeyRepeatModeResolver>();
        resolverMock.Setup(r => r.GetTargetState(true, updated)).Returns(updated.FastMode);
        resolverMock.Setup(r => r.GetModeName(true)).Returns("FastMode");

        var coordinator = CreateCoordinatorWithMockedResolver(initial, resolverMock);

        coordinator.OnSettingsChanged(newSettings);
        coordinator.UpdateRunningState(true);

        _applierMock.Verify(a => a.Apply(It.Is<KeyRepeatState>(s => s.RepeatSpeed == 31 && s.RepeatDelay == 250)),
            Times.Once);
    }
}