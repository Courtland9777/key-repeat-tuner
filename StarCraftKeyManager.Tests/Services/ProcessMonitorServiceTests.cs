using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using StarCraftKeyManager.Configuration;
using StarCraftKeyManager.Events;
using StarCraftKeyManager.Interfaces;
using StarCraftKeyManager.Services;
using StarCraftKeyManager.SystemAdapters.Interfaces;
using Xunit;

namespace StarCraftKeyManager.Tests.Services;

public class ProcessMonitorServiceTests
{
    private readonly Mock<IKeyRepeatSettingsService> _mockKeyRepeatSettingsService;
    private readonly Mock<ILogger<ProcessMonitorService>> _mockLogger;
    private readonly Mock<IProcessEventWatcher> _mockProcessEventWatcher;
    private readonly Mock<IProcessProvider> _mockProcessProvider;
    private readonly ProcessMonitorService _processMonitorService;

    public ProcessMonitorServiceTests()
    {
        _mockLogger = new Mock<ILogger<ProcessMonitorService>>();
        _mockProcessEventWatcher = new Mock<IProcessEventWatcher>();
        _mockKeyRepeatSettingsService = new Mock<IKeyRepeatSettingsService>();
        _mockProcessProvider = new Mock<IProcessProvider>();

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
        var optionsMonitor = mockOptionsMonitor.Object;

        _mockProcessProvider
            .Setup(p => p.GetProcessIdsByName("starcraft"))
            .Returns([]);

        _processMonitorService = new ProcessMonitorService(
            _mockLogger.Object,
            optionsMonitor,
            _mockProcessEventWatcher.Object,
            _mockProcessProvider.Object,
            _mockKeyRepeatSettingsService.Object
        );
    }

    [Fact]
    public void ProcessEventOccurred_ShouldApplySettings_WhenProcessStarts()
    {
        _processMonitorService.HandleProcessEvent(new ProcessEventArgs(ProcessEventId.Start, 1234, "starcraft.exe"));

        _mockKeyRepeatSettingsService.Verify(x => x.UpdateRunningState(true), Times.Once);
    }

    [Fact]
    public void ProcessEventOccurred_ShouldApplySettings_WhenLastProcessStops()
    {
        _processMonitorService.HandleProcessEvent(new ProcessEventArgs(ProcessEventId.Start, 1234, "starcraft.exe"));
        _mockKeyRepeatSettingsService.Invocations.Clear();

        _processMonitorService.HandleProcessEvent(new ProcessEventArgs(ProcessEventId.Stop, 1234, "starcraft.exe"));

        _mockKeyRepeatSettingsService.Verify(x => x.UpdateRunningState(false), Times.Once);
    }
}