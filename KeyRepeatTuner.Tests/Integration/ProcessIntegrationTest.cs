using KeyRepeatTuner.Configuration;
using KeyRepeatTuner.Configuration.ValueObjects;
using KeyRepeatTuner.Core.Interfaces;
using KeyRepeatTuner.Core.Services;
using KeyRepeatTuner.Monitoring.Services;
using KeyRepeatTuner.Tests.TestUtilities.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace KeyRepeatTuner.Tests.Integration;

public class ProcessIntegrationTest
{
    [Fact]
    public void ProcessStart_ShouldTriggerFastModeApplication()
    {
        // Arrange
        var mockApplier = new Mock<IKeyRepeatApplier>();
        var mockLogger = new Mock<ILogger<KeyRepeatModeCoordinator>>();
        var mockOptions = new Mock<IOptionsMonitor<AppSettings>>();
        var settings = new AppSettings
        {
            ProcessNames = [new ProcessName("starcraft")],
            KeyRepeat = new KeyRepeatSettings
            {
                Default = new KeyRepeatState { RepeatSpeed = 30, RepeatDelay = 800 },
                FastMode = new KeyRepeatState { RepeatSpeed = 10, RepeatDelay = 500 }
            }
        };
        mockOptions.Setup(o => o.CurrentValue).Returns(settings);

        var coordinator = new KeyRepeatModeCoordinator(
            mockLogger.Object,
            mockApplier.Object,
            mockOptions.Object,
            new KeyRepeatModeResolver());

        var router = new ProcessStateTracker(
            new Mock<ILogger<ProcessStateTracker>>().Object,
            coordinator);

        // Act
        router.OnProcessStarted(999, "starcraft.exe");

        // Assert
        mockApplier.Verify(a => a.Apply(It.Is<KeyRepeatState>(
            s => s.RepeatSpeed == 10 && s.RepeatDelay == 500)), Times.Once);

        mockLogger.VerifyLogContains(LogLevel.Information, "Mode=FastMode");
    }

    [Fact]
    public void ProcessStop_ShouldRevertToDefaultMode()
    {
        // Arrange
        var mockApplier = new Mock<IKeyRepeatApplier>();
        var mockLogger = new Mock<ILogger<KeyRepeatModeCoordinator>>();
        var mockOptions = new Mock<IOptionsMonitor<AppSettings>>();
        var settings = new AppSettings
        {
            ProcessNames = [new ProcessName("starcraft")],
            KeyRepeat = new KeyRepeatSettings
            {
                Default = new KeyRepeatState { RepeatSpeed = 30, RepeatDelay = 800 },
                FastMode = new KeyRepeatState { RepeatSpeed = 10, RepeatDelay = 500 }
            }
        };
        mockOptions.Setup(o => o.CurrentValue).Returns(settings);

        var coordinator = new KeyRepeatModeCoordinator(
            mockLogger.Object,
            mockApplier.Object,
            mockOptions.Object,
            new KeyRepeatModeResolver());

        var router = new ProcessStateTracker(
            new Mock<ILogger<ProcessStateTracker>>().Object,
            coordinator);

        // Simulate a process start first
        router.OnProcessStarted(1234, "starcraft.exe");

        // Act – simulate the same process exiting
        router.OnProcessStopped(1234, "starcraft.exe");

        // Assert
        mockApplier.Verify(a => a.Apply(It.Is<KeyRepeatState>(
            s => s.RepeatSpeed == 30 && s.RepeatDelay == 800)), Times.Once);

        mockLogger.VerifyLogContains(LogLevel.Information, "Mode=Default");
    }
}