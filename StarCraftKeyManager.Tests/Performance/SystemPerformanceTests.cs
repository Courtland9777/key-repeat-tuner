using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using StarCraftKeyManager.Configuration;
using StarCraftKeyManager.Events;
using StarCraftKeyManager.Interfaces;
using StarCraftKeyManager.Services;
using StarCraftKeyManager.SystemAdapters.Interfaces;
using Xunit;

namespace StarCraftKeyManager.Tests.Performance;

public class SystemPerformanceTests
{
    private readonly Mock<IKeyRepeatSettingsService> _mockKeyRepeatSettingsService;
    private readonly Mock<ILogger<ProcessStateTracker>> _mockLogger;
    private readonly Mock<IProcessEventWatcher> _mockProcessEventWatcher;
    private readonly Mock<IProcessProvider> _mockProcessProvider;
    private readonly ProcessStateTracker _processStateTracker;

    public SystemPerformanceTests()
    {
        _mockLogger = new Mock<ILogger<ProcessStateTracker>>();
        _mockProcessEventWatcher = new Mock<IProcessEventWatcher>();
        _mockProcessProvider = new Mock<IProcessProvider>();
        _mockKeyRepeatSettingsService = new Mock<IKeyRepeatSettingsService>();

        var settings = new AppSettings
        {
            ProcessName = "starcraft",
            KeyRepeat = new KeyRepeatSettings
            {
                Default = new KeyRepeatState { RepeatSpeed = 31, RepeatDelay = 1000 },
                FastMode = new KeyRepeatState { RepeatSpeed = 20, RepeatDelay = 500 }
            }
        };

        var mockOptionsMonitor = new Mock<IOptionsMonitor<AppSettings>>();
        mockOptionsMonitor.Setup(o => o.CurrentValue).Returns(settings);

        _mockProcessProvider.Setup(p => p.GetProcessIdsByName("starcraft")).Returns([]);

        _processStateTracker = new ProcessStateTracker(
            _mockLogger.Object,
            _mockKeyRepeatSettingsService.Object
        );
    }

    [Fact]
    public async Task ProcessMonitorService_ShouldHandleRapidEvents_Efficiently()
    {
        for (var i = 0; i < 100; i++)
            await _processStateTracker.Handle(
                new ProcessStarted(1000 + i, "starcraft.exe"),
                CancellationToken.None);

        for (var i = 0; i < 100; i++)
            await _processStateTracker.Handle(
                new ProcessStopped(1000 + i, "starcraft.exe"),
                CancellationToken.None);

        _mockKeyRepeatSettingsService.Verify(s => s.UpdateRunningState(true), Times.Once);
        _mockKeyRepeatSettingsService.Verify(s => s.UpdateRunningState(false), Times.Once);
    }
}