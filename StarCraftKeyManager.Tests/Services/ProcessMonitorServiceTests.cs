using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using StarCraftKeyManager.Adapters;
using StarCraftKeyManager.Configuration;
using StarCraftKeyManager.Events;
using StarCraftKeyManager.Interfaces;
using StarCraftKeyManager.Models;
using StarCraftKeyManager.Services;
using Xunit;

namespace StarCraftKeyManager.Tests.Services;

public class ProcessMonitorServiceTests
{
    private readonly Mock<IKeyboardSettingsApplier> _mockKeyboardSettingsApplier;
    private readonly Mock<ILogger<ProcessMonitorService>> _mockLogger;
    private readonly Mock<IProcessEventWatcher> _mockProcessEventWatcher;
    private readonly Mock<IProcessProvider> _mockProcessProvider;
    private readonly ProcessMonitorService _processMonitorService;

    public ProcessMonitorServiceTests()
    {
        _mockLogger = new Mock<ILogger<ProcessMonitorService>>();
        _mockProcessEventWatcher = new Mock<IProcessEventWatcher>();
        _mockKeyboardSettingsApplier = new Mock<IKeyboardSettingsApplier>();
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
            _mockKeyboardSettingsApplier.Object,
            _mockProcessProvider.Object
        );
    }

    [Fact]
    public async Task StartAsync_ShouldStartMonitoring_WithoutErrors()
    {
        await _processMonitorService.StartAsync(CancellationToken.None);
        await _processMonitorService.StopAsync(CancellationToken.None);

        _mockLogger.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ProcessEventOccurred_ShouldApplyKeyRepeatSettings_WhenProcessStarts()
    {
        await _processMonitorService.StartAsync(CancellationToken.None);

        _mockProcessEventWatcher.Raise(
            w => w.ProcessEventOccurred += null,
            new ProcessEventArgs(4688, 1234, "starcraft.exe")
        );

        _mockLogger.Verify(log => log.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<object>(o => o.ToString()!.Contains("Applying key repeat settings: FastMode")),
            null,
            It.IsAny<Func<object, Exception?, string>>()
        ), Times.Once);
    }

    [Fact]
    public async Task StartAsync_ShouldDetectProcessMonitoring_WhenInvoked()
    {
        await _processMonitorService.StartAsync(CancellationToken.None);

        _mockLogger.Verify(log => log.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<object>(o => o.ToString()!.Contains("Monitoring process: starcraft.exe")),
            null,
            It.IsAny<Func<object, Exception?, string>>()
        ), Times.Once);
    }

    [Fact]
    public async Task ProcessEventOccurred_ShouldDetectProcessExit_AndRestoreDefaultSettings()
    {
        await _processMonitorService.StartAsync(CancellationToken.None);

        _mockProcessEventWatcher.Raise(
            w => w.ProcessEventOccurred += null,
            new ProcessEventArgs(4689, 1234, "starcraft.exe")
        );

        _mockLogger.Verify(log => log.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<object>(o => o.ToString()!.Contains("Restoring default key repeat settings")),
            null,
            It.IsAny<Func<object, Exception?, string>>()
        ), Times.Once);
    }

    [Fact]
    public async Task ProcessEventOccurred_ShouldNotApplySettings_WhenProcessNameDoesNotMatch()
    {
        await _processMonitorService.StartAsync(CancellationToken.None);

        _mockProcessEventWatcher.Raise(
            w => w.ProcessEventOccurred += null,
            new ProcessEventArgs(4688, 1234, "notepad.exe")
        );

        _mockLogger.Verify(log => log.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<object>(o => o.ToString()!.Contains("Applying key repeat settings")),
            null,
            It.IsAny<Func<object, Exception?, string>>()
        ), Times.Never);
    }

    [Fact]
    public async Task StartAsync_ShouldApplyFastMode_WhenStarCraftIsAlreadyRunning()
    {
        _mockProcessEventWatcher.Raise(
            w => w.ProcessEventOccurred += null,
            new ProcessEventArgs(4688, 1234, "starcraft.exe")
        );

        await _processMonitorService.StartAsync(CancellationToken.None);

        _mockLogger.Verify(log => log.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<object>(o => o.ToString()!.Contains("Applying key repeat settings: FastMode")),
            null,
            It.IsAny<Func<object, Exception?, string>>()
        ), Times.Once);
    }

    [Fact]
    public async Task ProcessEventOccurred_ShouldHandleMultipleInstances_Correctly()
    {
        await _processMonitorService.StartAsync(CancellationToken.None);

        _mockProcessEventWatcher.Raise(
            w => w.ProcessEventOccurred += null,
            new ProcessEventArgs(4688, 1234, "starcraft.exe")
        );
        _mockProcessEventWatcher.Raise(
            w => w.ProcessEventOccurred += null,
            new ProcessEventArgs(4688, 5678, "starcraft.exe")
        );

        _mockLogger.Verify(log => log.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<object>(o => o.ToString()!.Contains("Applying key repeat settings: FastMode")),
            null,
            It.IsAny<Func<object, Exception?, string>>()
        ), Times.Once);
    }

    [Fact]
    public async Task ProcessEventOccurred_ShouldRestoreDefaultSettings_WhenAllInstancesExit()
    {
        await _processMonitorService.StartAsync(CancellationToken.None);

        _mockProcessEventWatcher.Raise(
            w => w.ProcessEventOccurred += null,
            new ProcessEventArgs(4688, 1234, "starcraft.exe")
        );
        _mockProcessEventWatcher.Raise(
            w => w.ProcessEventOccurred += null,
            new ProcessEventArgs(4688, 5678, "starcraft.exe")
        );

        _mockProcessEventWatcher.Raise(
            w => w.ProcessEventOccurred += null,
            new ProcessEventArgs(4689, 1234, "starcraft.exe")
        );
        _mockProcessEventWatcher.Raise(
            w => w.ProcessEventOccurred += null,
            new ProcessEventArgs(4689, 5678, "starcraft.exe")
        );

        _mockLogger.Verify(log => log.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<object>(o => o.ToString()!.Contains("Restoring default key repeat settings")),
            null,
            It.IsAny<Func<object, Exception?, string>>()
        ), Times.Once);
    }

    [Fact]
    public async Task StartAsync_ShouldRecoverGracefully_AfterUnexpectedShutdown()
    {
        await _processMonitorService.StartAsync(CancellationToken.None);

        await _processMonitorService.StopAsync(CancellationToken.None);

        await _processMonitorService.StartAsync(CancellationToken.None);

        _mockLogger.Verify(log => log.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<object>(o => o.ToString()!.Contains("Process monitor service started")),
            null,
            It.IsAny<Func<object, Exception?, string>>()
        ), Times.Exactly(2));
    }

    [Fact]
    public async Task ProcessEventOccurred_ShouldHandleRapidStartsAndStops_WithoutErrors()
    {
        await _processMonitorService.StartAsync(CancellationToken.None);

        for (var i = 0; i < 10; i++)
        {
            _mockProcessEventWatcher.Raise(
                w => w.ProcessEventOccurred += null,
                new ProcessEventArgs(4688, i, "starcraft.exe")
            );

            _mockProcessEventWatcher.Raise(
                w => w.ProcessEventOccurred += null,
                new ProcessEventArgs(4689, i, "starcraft.exe")
            );
        }

        _mockLogger.Verify(log => log.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<object>(o => o.ToString()!.Contains("Applying key repeat settings")),
            null,
            It.IsAny<Func<object, Exception?, string>>()
        ), Times.AtMost(2));
    }
}