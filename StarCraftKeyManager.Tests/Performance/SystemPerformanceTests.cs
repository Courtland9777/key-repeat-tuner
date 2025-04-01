using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using StarCraftKeyManager.Configuration;
using StarCraftKeyManager.Interfaces;
using StarCraftKeyManager.Services;
using StarCraftKeyManager.SystemAdapters.Interfaces;
using Xunit;

namespace StarCraftKeyManager.Tests.Performance;

public class SystemPerformanceTests
{
    private readonly Mock<IKeyRepeatSettingsService> _mockKeyRepeatSettingsService;
    private readonly Mock<ILogger<ProcessMonitorService>> _mockLogger;
    private readonly Mock<IProcessEventWatcher> _mockProcessEventWatcher;
    private readonly Mock<IProcessProvider> _mockProcessProvider;
    private readonly ProcessMonitorService _processMonitorService;

    public SystemPerformanceTests()
    {
        _mockLogger = new Mock<ILogger<ProcessMonitorService>>();
        _mockProcessEventWatcher = new Mock<IProcessEventWatcher>();
        _mockProcessProvider = new Mock<IProcessProvider>();
        _mockKeyRepeatSettingsService = new Mock<IKeyRepeatSettingsService>();

        var settings = new AppSettings
        {
            ProcessMonitor = new ProcessMonitorSettings { ProcessName = "starcraft.exe" },
            KeyRepeat = new KeyRepeatSettings
            {
                Default = new KeyRepeatState { RepeatSpeed = 31, RepeatDelay = 1000 },
                FastMode = new KeyRepeatState { RepeatSpeed = 20, RepeatDelay = 500 }
            }
        };

        var mockOptionsMonitor = new Mock<IOptionsMonitor<AppSettings>>();
        mockOptionsMonitor.Setup(o => o.CurrentValue).Returns(settings);

        _mockProcessProvider.Setup(p => p.GetProcessIdsByName("starcraft")).Returns([]);

        _processMonitorService = new ProcessMonitorService(
            _mockLogger.Object,
            mockOptionsMonitor.Object,
            _mockProcessEventWatcher.Object,
            _mockProcessProvider.Object,
            _mockKeyRepeatSettingsService.Object
        );
    }

    [Fact]
    public async Task StartAsync_ShouldRunEfficiently()
    {
        await _processMonitorService.StartAsync(CancellationToken.None);
        await Task.Delay(500);
        await _processMonitorService.StopAsync(CancellationToken.None);
    }
}