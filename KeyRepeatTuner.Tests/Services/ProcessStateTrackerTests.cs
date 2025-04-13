using KeyRepeatTuner.Interfaces;
using KeyRepeatTuner.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace KeyRepeatTuner.Tests.Services;

public class ProcessStateTrackerTests
{
    private readonly Mock<IKeyRepeatSettingsService> _mockKeyRepeatSettingsService;
    private readonly ProcessStateTracker _tracker;

    public ProcessStateTrackerTests()
    {
        _mockKeyRepeatSettingsService = new Mock<IKeyRepeatSettingsService>();
        var mockLogger = new Mock<ILogger<ProcessStateTracker>>();

        _tracker = new ProcessStateTracker(
            mockLogger.Object,
            _mockKeyRepeatSettingsService.Object
        );
    }

    [Fact]
    public void OnProcessStarted_WhenNoOtherProcesses_ShouldEnableFastMode()
    {
        _tracker.OnProcessStarted(5678, "game.exe");

        _mockKeyRepeatSettingsService.Verify(s => s.UpdateRunningState(true), Times.Once);
    }

    [Fact]
    public void OnProcessStarted_WhenMultipleProcesses_ShouldOnlyEnableFastModeOnce()
    {
        _tracker.OnProcessStarted(1111, "a.exe");
        _tracker.OnProcessStarted(2222, "b.exe");
        _tracker.OnProcessStarted(3333, "c.exe");

        _mockKeyRepeatSettingsService.Verify(s => s.UpdateRunningState(true), Times.Once);
    }

    [Fact]
    public void OnProcessStopped_WhenAllProcessesStopped_ShouldDisableFastMode()
    {
        _tracker.OnProcessStarted(1234, "first.exe");
        _tracker.OnProcessStopped(1234, "first.exe");

        _mockKeyRepeatSettingsService.Verify(s => s.UpdateRunningState(true), Times.Once);
        _mockKeyRepeatSettingsService.Verify(s => s.UpdateRunningState(false), Times.Once);
    }

    [Fact]
    public void OnProcessStopped_WhenSomeProcessesStillRunning_ShouldNotDisableFastMode()
    {
        _tracker.OnProcessStarted(1, "one.exe");
        _tracker.OnProcessStarted(2, "two.exe");

        _tracker.OnProcessStopped(1, "one.exe");

        _mockKeyRepeatSettingsService.Verify(s => s.UpdateRunningState(true), Times.Once);
        _mockKeyRepeatSettingsService.Verify(s => s.UpdateRunningState(false), Times.Never);
    }

    [Fact]
    public void OnProcessStopped_WhenLastProcessEnds_ShouldDisableFastMode()
    {
        _tracker.OnProcessStarted(1, "one.exe");
        _tracker.OnProcessStarted(2, "two.exe");

        _tracker.OnProcessStopped(1, "one.exe");
        _tracker.OnProcessStopped(2, "two.exe");

        _mockKeyRepeatSettingsService.Verify(s => s.UpdateRunningState(true), Times.Once);
        _mockKeyRepeatSettingsService.Verify(s => s.UpdateRunningState(false), Times.Once);
    }

    [Fact]
    public void OnStartup_ShouldDoNothing()
    {
        // Just call and verify no exceptions or side effects
        _tracker.OnStartup();

        _mockKeyRepeatSettingsService.VerifyNoOtherCalls();
    }

    [Fact]
    public void RedundantStopCalls_ShouldNotThrowOrChangeState()
    {
        _tracker.OnProcessStarted(1, "a.exe");
        _tracker.OnProcessStopped(1, "a.exe");

        // These redundant stops should not throw or re-trigger anything
        _tracker.OnProcessStopped(1, "a.exe");
        _tracker.OnProcessStopped(2, "b.exe");

        _mockKeyRepeatSettingsService.Verify(s => s.UpdateRunningState(true), Times.Once);
        _mockKeyRepeatSettingsService.Verify(s => s.UpdateRunningState(false), Times.Once);
    }
}