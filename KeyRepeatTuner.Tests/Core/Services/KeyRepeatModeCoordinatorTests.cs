using KeyRepeatTuner.Configuration;
using KeyRepeatTuner.Configuration.ValueObjects;
using KeyRepeatTuner.Core.Interfaces;
using KeyRepeatTuner.Core.Services;
using KeyRepeatTuner.Tests.TestUtilities.Stubs;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace KeyRepeatTuner.Tests.Core.Services;

public class KeyRepeatModeCoordinatorTests
{
    private readonly Mock<IKeyRepeatApplier> _applierMock = new();

    private readonly KeyRepeatModeCoordinator _coordinator;
    private readonly Mock<ILogger<KeyRepeatModeCoordinator>> _loggerMock = new();
    private readonly Mock<IKeyRepeatModeResolver> _resolverMock = new();

    private readonly KeyRepeatSettings _settings = new()
    {
        Default = new KeyRepeatState { RepeatSpeed = 20, RepeatDelay = 1000 },
        FastMode = new KeyRepeatState { RepeatSpeed = 10, RepeatDelay = 500 }
    };

    public KeyRepeatModeCoordinatorTests()
    {
        var optionsMonitor = new TestOptionsMonitor<AppSettings>(new AppSettings
        {
            ProcessNames = [new ProcessName("dummy")],
            KeyRepeat = _settings
        });

        _coordinator = new KeyRepeatModeCoordinator(
            _loggerMock.Object,
            _applierMock.Object,
            optionsMonitor,
            _resolverMock.Object
        );
    }

    [Theory]
    [InlineData(true, "FastMode", 10, 500)]
    [InlineData(false, "Default", 20, 1000)]
    public void UpdateRunningState_ShouldApplyCorrectSettings(bool isRunning, string expectedMode, int expectedSpeed,
        int expectedDelay)
    {
        // Arrange
        var expectedState = new KeyRepeatState { RepeatSpeed = expectedSpeed, RepeatDelay = expectedDelay };
        _resolverMock.Setup(x => x.GetTargetState(isRunning, _settings)).Returns(expectedState);
        _resolverMock.Setup(x => x.GetModeName(isRunning)).Returns(expectedMode);

        // Act
        _coordinator.UpdateRunningState(isRunning);

        // Assert
        _applierMock.Verify(x => x.Apply(It.Is<KeyRepeatState>(s =>
            s.RepeatSpeed == expectedSpeed && s.RepeatDelay == expectedDelay)), Times.Once);
    }

    [Fact]
    public void OnSettingsChanged_ShouldUpdateInternalSettings()
    {
        // Arrange
        var newSettings = new AppSettings
        {
            ProcessNames = [new ProcessName("newprocess")],
            KeyRepeat = new KeyRepeatSettings
            {
                Default = new KeyRepeatState { RepeatSpeed = 5, RepeatDelay = 300 },
                FastMode = new KeyRepeatState { RepeatSpeed = 31, RepeatDelay = 250 }
            }
        };

        // Act
        _coordinator.OnSettingsChanged(newSettings);

        // Now force an update to trigger the new FastMode
        _resolverMock.Setup(r => r.GetTargetState(true, newSettings.KeyRepeat))
            .Returns(newSettings.KeyRepeat.FastMode);
        _resolverMock.Setup(r => r.GetModeName(true)).Returns("FastMode");

        _coordinator.UpdateRunningState(true);

        // Assert
        _applierMock.Verify(x => x.Apply(It.Is<KeyRepeatState>(s =>
            s.RepeatSpeed == 31 && s.RepeatDelay == 250)), Times.Once);
    }
}