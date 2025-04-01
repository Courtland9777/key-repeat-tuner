using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using StarCraftKeyManager.Configuration;
using StarCraftKeyManager.Events;
using StarCraftKeyManager.Interfaces;
using StarCraftKeyManager.Services;
using StarCraftKeyManager.SystemAdapters.Interfaces;
using StarCraftKeyManager.Tests.TestUtilities.Extensions;
using Xunit;

namespace StarCraftKeyManager.Tests.Integration;

public class ProcessMonitorServiceIntegrationTests
{
    private readonly Mock<IKeyRepeatSettingsService> _mockKeyRepeatSettingsService;
    private readonly Mock<ILogger<ProcessMonitorService>> _mockLogger;
    private readonly Mock<IProcessEventWatcher> _mockProcessEventWatcher;
    private readonly ProcessMonitorService _processMonitorService;

    public ProcessMonitorServiceIntegrationTests()
    {
        _mockLogger = new Mock<ILogger<ProcessMonitorService>>();
        _mockProcessEventWatcher = new Mock<IProcessEventWatcher>();
        _mockKeyRepeatSettingsService = new Mock<IKeyRepeatSettingsService>();
        var mockProcessProvider = new Mock<IProcessProvider>();

        var mockSettings = new AppSettings
        {
            ProcessName = "starcraft",
            KeyRepeat = new KeyRepeatSettings
            {
                Default = new KeyRepeatState { RepeatSpeed = 31, RepeatDelay = 1000 },
                FastMode = new KeyRepeatState { RepeatSpeed = 20, RepeatDelay = 500 }
            }
        };

        var mockOptionsMonitor = new Mock<IOptionsMonitor<AppSettings>>();
        mockOptionsMonitor.Setup(o => o.CurrentValue).Returns(mockSettings);

        mockProcessProvider
            .Setup(p => p.GetProcessIdsByName("starcraft"))
            .Returns([]);

        _processMonitorService = new ProcessMonitorService(
            _mockLogger.Object,
            _mockKeyRepeatSettingsService.Object
        );
    }

    [Fact]
    public async Task ProcessStarted_ShouldTriggerStateChange_AndLog()
    {
        var evt = new ProcessStarted(1234, "starcraft.exe");

        await _processMonitorService.Handle(evt, CancellationToken.None);

        _mockKeyRepeatSettingsService.Verify(s => s.UpdateRunningState(true), Times.Once);

        _mockLogger.Verify(log => log.Log(
            LogLevel.Debug,
            It.IsAny<EventId>(),
            MoqLogExtensions.MatchLogState("Process started: PID=1234, Name=starcraft.exe"),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);

        _mockLogger.Verify(log => log.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            MoqLogExtensions.MatchLogState("Process running state changed to: True"),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact]
    public async Task ProcessStopped_ShouldTriggerStateChange_AndLog()
    {
        // Pre-fill state with a running process
        await _processMonitorService.Handle(new ProcessStarted(1234, "sc2.exe"), CancellationToken.None);
        _mockKeyRepeatSettingsService.Invocations.Clear();

        // Then stop it
        var evt = new ProcessStopped(1234, "sc2.exe");

        await _processMonitorService.Handle(evt, CancellationToken.None);

        _mockKeyRepeatSettingsService.Verify(s => s.UpdateRunningState(false), Times.Once);

        _mockLogger.Verify(log => log.Log(
            LogLevel.Debug,
            It.IsAny<EventId>(),
            MoqLogExtensions.MatchLogState("Process stopped: PID=1234, Name=sc2.exe"),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);

        _mockLogger.Verify(log => log.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            MoqLogExtensions.MatchLogState("Process running state changed to: False"),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact]
    public async Task ProcessEvents_ShouldTriggerSettingsChange_WhenStarCraftStartsAndStops()
    {
        // Act: simulate process start
        await _processMonitorService.Handle(
            new ProcessStarted(1234, "starcraft.exe"),
            CancellationToken.None);

        // Assert: settings switched to FastMode
        _mockKeyRepeatSettingsService.Verify(s => s.UpdateRunningState(true), Times.Once);

        // Act: simulate process stop
        await _processMonitorService.Handle(
            new ProcessStopped(1234, "starcraft.exe"),
            CancellationToken.None);

        // Assert: settings switched back to Default
        _mockKeyRepeatSettingsService.Verify(s => s.UpdateRunningState(false), Times.Once);

        // Assert: logging occurred
        _mockLogger.Verify(log => log.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            MoqLogExtensions.MatchLogState("Process running state changed to: False"),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);

        // Assert: call sequence
        var sequence = _mockKeyRepeatSettingsService.Invocations
            .Where(i => i.Method.Name == nameof(IKeyRepeatSettingsService.UpdateRunningState))
            .Select(i => (bool)i.Arguments[0])
            .ToArray();

        Assert.Equal([true, false], sequence);
    }
}