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
            ProcessMonitor = new ProcessMonitorSettings { ProcessName = "starcraft.exe" },
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
            mockOptionsMonitor.Object,
            _mockProcessEventWatcher.Object,
            mockProcessProvider.Object,
            _mockKeyRepeatSettingsService.Object
        );
    }

    [Fact]
    public async Task StartAsync_ShouldBeginMonitoring_WhenInvoked()
    {
        var cts = new CancellationTokenSource();

        var monitorTask = _processMonitorService.StartAsync(cts.Token);
        await Task.Delay(500, cts.Token);

        _mockLogger.Verify(log => log.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            MoqLogExtensions.MatchLogState("Starting process monitor service."),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);

        await cts.CancelAsync();
        await monitorTask;
    }

    [Fact]
    public async Task ProcessEventOccurred_ShouldTriggerSettingsChange_WhenStarCraftStartsAndStops()
    {
        await _processMonitorService.StartAsync(CancellationToken.None);

        _mockProcessEventWatcher.Raise(
            w => w.ProcessEventOccurred += null,
            new ProcessEventArgs(ProcessEventId.Start, 1234, "starcraft.exe"));

        _mockKeyRepeatSettingsService.Verify(s => s.UpdateRunningState(true), Times.Once);

        _mockProcessEventWatcher.Raise(
            w => w.ProcessEventOccurred += null,
            new ProcessEventArgs(ProcessEventId.Stop, 1234, "starcraft.exe"));

        _mockKeyRepeatSettingsService.Verify(s => s.UpdateRunningState(false), Times.Once);

        _mockLogger.Verify(log => log.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            MoqLogExtensions.MatchLogState("Process running state changed to False. Updating key repeat settings..."),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }
}