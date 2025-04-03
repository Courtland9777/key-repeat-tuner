using KeyRepeatTuner.Events;
using KeyRepeatTuner.Interfaces;
using KeyRepeatTuner.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace KeyRepeatTuner.Tests.Services;

public class ProcessStateTrackerTests
{
    private readonly Mock<IKeyRepeatSettingsService> _mockKeyRepeatSettingsService;
    private readonly ProcessStateTracker _processStateTracker;

    public ProcessStateTrackerTests()
    {
        var mockLogger = new Mock<ILogger<ProcessStateTracker>>();
        _mockKeyRepeatSettingsService = new Mock<IKeyRepeatSettingsService>();

        _processStateTracker = new ProcessStateTracker(
            mockLogger.Object,
            _mockKeyRepeatSettingsService.Object);
    }

    [Fact]
    public async Task Handle_ShouldApplySettings_WhenProcessStarts()
    {
        await _processStateTracker.Handle(
            new ProcessStarted(1234, "starcraft.exe"),
            CancellationToken.None);

        _mockKeyRepeatSettingsService.Verify(x => x.UpdateRunningState(true), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldApplySettings_WhenLastProcessStops()
    {
        // Start -> triggers running state
        await _processStateTracker.Handle(
            new ProcessStarted(1234, "starcraft.exe"),
            CancellationToken.None);
        _mockKeyRepeatSettingsService.Invocations.Clear();

        // Stop -> no more processes running
        await _processStateTracker.Handle(
            new ProcessStopped(1234, "starcraft.exe"),
            CancellationToken.None);

        _mockKeyRepeatSettingsService.Verify(x => x.UpdateRunningState(false), Times.Once);
    }
}