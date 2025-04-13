using KeyRepeatTuner.Interfaces;
using KeyRepeatTuner.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace KeyRepeatTuner.Tests.Integration;

public class ProcessStateTrackerIntegrationTests
{
    private readonly Mock<IKeyRepeatSettingsService> _mockKeyRepeatSettingsService;
    private readonly ProcessStateTracker _processStateTracker;

    public ProcessStateTrackerIntegrationTests()
    {
        var mockLogger = new Mock<ILogger<ProcessStateTracker>>();
        _mockKeyRepeatSettingsService = new Mock<IKeyRepeatSettingsService>();

        _processStateTracker = new ProcessStateTracker(
            mockLogger.Object,
            _mockKeyRepeatSettingsService.Object
        );
    }

    [Fact]
    public void OnProcessStarted_ShouldTriggerUpdateRunningState_True()
    {
        _processStateTracker.OnProcessStarted(1234, "starcraft.exe");

        _mockKeyRepeatSettingsService.Verify(s => s.UpdateRunningState(true), Times.Once);
    }

    [Fact]
    public void OnProcessStopped_ShouldTriggerUpdateRunningState_False()
    {
        // Simulate process started first
        _processStateTracker.OnProcessStarted(1234, "starcraft.exe");
        _mockKeyRepeatSettingsService.Invocations.Clear(); // reset call tracking

        // Now stop it
        _processStateTracker.OnProcessStopped(1234, "starcraft.exe");

        _mockKeyRepeatSettingsService.Verify(s => s.UpdateRunningState(false), Times.Once);
    }

    [Fact]
    public void MultipleStartsAndStops_ShouldOnlyUpdateStateOnActualChange()
    {
        var pids = new[] { 1111, 2222, 3333 };

        foreach (var pid in pids)
            _processStateTracker.OnProcessStarted(pid, $"test_{pid}.exe");

        _mockKeyRepeatSettingsService.Verify(s => s.UpdateRunningState(true), Times.Once);

        _mockKeyRepeatSettingsService.Invocations.Clear();

        foreach (var pid in pids)
            _processStateTracker.OnProcessStopped(pid, $"test_{pid}.exe");

        _mockKeyRepeatSettingsService.Verify(s => s.UpdateRunningState(false), Times.Once);
    }

    [Fact]
    public void RepeatedStartsAndStops_ShouldTriggerStateChangesEachTime()
    {
        for (var i = 0; i < 5; i++)
        {
            _processStateTracker.OnProcessStarted(9999, "repeat.exe");
            _processStateTracker.OnProcessStopped(9999, "repeat.exe");
        }

        _mockKeyRepeatSettingsService.Verify(s => s.UpdateRunningState(true), Times.Exactly(5));
        _mockKeyRepeatSettingsService.Verify(s => s.UpdateRunningState(false), Times.Exactly(5));
    }
}