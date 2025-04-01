using Microsoft.Extensions.Logging;
using Moq;
using StarCraftKeyManager.Events;
using StarCraftKeyManager.Interfaces;
using StarCraftKeyManager.Services;
using Xunit;

namespace StarCraftKeyManager.Tests.Services;

public class ProcessMonitorServiceTests
{
    private readonly Mock<IKeyRepeatSettingsService> _mockKeyRepeatSettingsService;
    private readonly ProcessMonitorService _processMonitorService;

    public ProcessMonitorServiceTests()
    {
        var mockLogger = new Mock<ILogger<ProcessMonitorService>>();
        _mockKeyRepeatSettingsService = new Mock<IKeyRepeatSettingsService>();

        _processMonitorService = new ProcessMonitorService(
            mockLogger.Object,
            _mockKeyRepeatSettingsService.Object);
    }

    [Fact]
    public async Task Handle_ShouldApplySettings_WhenProcessStarts()
    {
        await _processMonitorService.Handle(
            new ProcessStarted(1234, "starcraft.exe"),
            CancellationToken.None);

        _mockKeyRepeatSettingsService.Verify(x => x.UpdateRunningState(true), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldApplySettings_WhenLastProcessStops()
    {
        // Start -> triggers running state
        await _processMonitorService.Handle(
            new ProcessStarted(1234, "starcraft.exe"),
            CancellationToken.None);
        _mockKeyRepeatSettingsService.Invocations.Clear();

        // Stop -> no more processes running
        await _processMonitorService.Handle(
            new ProcessStopped(1234, "starcraft.exe"),
            CancellationToken.None);

        _mockKeyRepeatSettingsService.Verify(x => x.UpdateRunningState(false), Times.Once);
    }
}