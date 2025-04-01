using Microsoft.Extensions.Logging;
using Moq;
using StarCraftKeyManager.Events;
using StarCraftKeyManager.Interfaces;
using StarCraftKeyManager.Services;
using Xunit;

namespace StarCraftKeyManager.Tests.Integration;

public class ProcessMonitorServiceIntegrationTests
{
    private readonly Mock<IKeyRepeatSettingsService> _mockKeyRepeatSettingsService;
    private readonly ProcessStateTracker _processStateTracker;

    public ProcessMonitorServiceIntegrationTests()
    {
        var mockLogger = new Mock<ILogger<ProcessStateTracker>>();
        _mockKeyRepeatSettingsService = new Mock<IKeyRepeatSettingsService>();

        _processStateTracker = new ProcessStateTracker(
            mockLogger.Object,
            _mockKeyRepeatSettingsService.Object
        );
    }

    [Fact]
    public async Task ProcessStarted_ShouldTriggerUpdateRunningState_True()
    {
        var evt = new ProcessStarted(1234, "starcraft.exe");

        await _processStateTracker.Handle(evt, CancellationToken.None);

        _mockKeyRepeatSettingsService.Verify(s => s.UpdateRunningState(true), Times.Once);
    }

    [Fact]
    public async Task ProcessStopped_ShouldTriggerUpdateRunningState_False()
    {
        // Simulate a process started first
        await _processStateTracker.Handle(new ProcessStarted(1234, "starcraft.exe"), CancellationToken.None);
        _mockKeyRepeatSettingsService.Invocations.Clear(); // reset call tracking

        // Now stop it
        await _processStateTracker.Handle(new ProcessStopped(1234, "starcraft.exe"), CancellationToken.None);

        _mockKeyRepeatSettingsService.Verify(s => s.UpdateRunningState(false), Times.Once);
    }

    [Fact]
    public async Task MultipleStartsAndStops_ShouldOnlyUpdateStateOnActualChange()
    {
        var starts = new[] { 1111, 2222, 3333 };
        var stops = new[] { 1111, 2222, 3333 };

        foreach (var pid in starts)
            await _processStateTracker.Handle(new ProcessStarted(pid, $"test_{pid}.exe"), CancellationToken.None);

        _mockKeyRepeatSettingsService.Verify(s => s.UpdateRunningState(true), Times.Once);

        _mockKeyRepeatSettingsService.Invocations.Clear();

        foreach (var pid in stops)
            await _processStateTracker.Handle(new ProcessStopped(pid, $"test_{pid}.exe"), CancellationToken.None);

        _mockKeyRepeatSettingsService.Verify(s => s.UpdateRunningState(false), Times.Once);
    }

    [Fact]
    public async Task RepeatedStartsAndStops_ShouldTriggerStateChangesEachTime()
    {
        for (var i = 0; i < 5; i++)
        {
            await _processStateTracker.Handle(new ProcessStarted(9999, "repeat.exe"), CancellationToken.None);
            await _processStateTracker.Handle(new ProcessStopped(9999, "repeat.exe"), CancellationToken.None);
        }

        // Each start/stop changes state, so both are called 5 times
        _mockKeyRepeatSettingsService.Verify(s => s.UpdateRunningState(true), Times.Exactly(5));
        _mockKeyRepeatSettingsService.Verify(s => s.UpdateRunningState(false), Times.Exactly(5));
    }
}