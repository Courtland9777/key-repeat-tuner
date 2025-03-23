using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using StarCraftKeyManager.Interfaces;
using StarCraftKeyManager.Models;
using StarCraftKeyManager.Services;
using StarCraftKeyManager.Tests.MoqExtensions;
using Xunit;

namespace StarCraftKeyManager.Tests.Integration;

public class ProcessMonitorIntegrationTests
{
    private readonly Mock<ILogger<ProcessMonitorService>> _mockLogger;
    private readonly Mock<IProcessEventWatcher> _mockProcessEventWatcher;
    private ProcessMonitorService _processMonitorService;

    public ProcessMonitorIntegrationTests()
    {
        _mockLogger = new Mock<ILogger<ProcessMonitorService>>();
        _mockProcessEventWatcher = new Mock<IProcessEventWatcher>();

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

        _processMonitorService = new ProcessMonitorService(
            _mockLogger.Object,
            optionsMonitor,
            _mockProcessEventWatcher.Object
        );
    }

    [Fact]
    public async Task StartAsync_ShouldBeginMonitoring_WhenInvoked()
    {
        var cts = new CancellationTokenSource();

        var monitorTask = _processMonitorService.StartAsync(cts.Token);

        await Task.Delay(500, cts.Token); // allow some async time

        _mockLogger.Verify(log => log.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                MoqLogExtensions.MatchLogState("Starting process monitor service"),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        await cts.CancelAsync();
        await monitorTask;
    }


    [Fact]
    public async Task ProcessEventOccurred_ShouldApplyFastMode_WhenStarCraftStarts()
    {
        // Act
        await _processMonitorService.StartAsync(CancellationToken.None);

        _mockProcessEventWatcher.Raise(
            w => w.ProcessEventOccurred += null,
            new ProcessEventArgs(4688, 1234, "starcraft.exe")
        );

        // Assert
        _mockLogger.Verify(log => log.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                MoqLogExtensions.MatchLogState(
                    "Applying key repeat settings: StarCraftKeyManager.Models.KeyRepeatState"),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }


    [Fact]
    public async Task ProcessEventOccurred_ShouldRestoreDefaultSettings_WhenStarCraftStops()
    {
        // Arrange: configure settings with a valid process name
        var settings = new AppSettings
        {
            ProcessMonitor = new ProcessMonitorSettings { ProcessName = "starcraft.exe" },
            KeyRepeat = new KeyRepeatSettings
            {
                Default = new KeyRepeatState { RepeatSpeed = 25, RepeatDelay = 750 },
                FastMode = new KeyRepeatState { RepeatSpeed = 15, RepeatDelay = 400 }
            }
        };

        var optionsMonitor = new TestOptionsMonitor<AppSettings>(settings);
        _processMonitorService = new ProcessMonitorService(
            _mockLogger.Object,
            optionsMonitor,
            _mockProcessEventWatcher.Object
        );

        await _processMonitorService.StartAsync(CancellationToken.None);
        _mockLogger.Invocations.Clear(); // isolate log output to what we trigger next

        // Act: simulate process start and stop
        _mockProcessEventWatcher.Raise(
            w => w.ProcessEventOccurred += null,
            new ProcessEventArgs(4688, 1234, "starcraft.exe")
        );
        _mockProcessEventWatcher.Raise(
            w => w.ProcessEventOccurred += null,
            new ProcessEventArgs(4689, 1234, "starcraft.exe")
        );

        // Assert: default settings should be restored
        _mockLogger.Verify(log => log.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                MoqLogExtensions.MatchLogState("Process running state changed to False"),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }


    [Fact]
    public void ConfigurationUpdated_ShouldApplyNewSettings_WhenConfigurationChanges()
    {
        // Arrange
        var initialSettings = new AppSettings
        {
            ProcessMonitor = new ProcessMonitorSettings { ProcessName = "starcraft.exe" },
            KeyRepeat = new KeyRepeatSettings
            {
                Default = new KeyRepeatState { RepeatSpeed = 31, RepeatDelay = 1000 },
                FastMode = new KeyRepeatState { RepeatSpeed = 20, RepeatDelay = 500 }
            }
        };

        var updatedSettings = new AppSettings
        {
            ProcessMonitor = new ProcessMonitorSettings { ProcessName = "newgame.exe" },
            KeyRepeat = new KeyRepeatSettings
            {
                Default = new KeyRepeatState { RepeatSpeed = 25, RepeatDelay = 750 },
                FastMode = new KeyRepeatState { RepeatSpeed = 15, RepeatDelay = 400 }
            }
        };

        var optionsMonitor = new TestOptionsMonitor<AppSettings>(initialSettings);
        var mockLogger = new Mock<ILogger<ProcessMonitorService>>();
        var mockWatcher = new Mock<IProcessEventWatcher>();

        // Act: construct the service (registers for changes)
        var _ = new ProcessMonitorService(
            mockLogger.Object,
            optionsMonitor,
            mockWatcher.Object
        );

        // Trigger the change manually
        optionsMonitor.TriggerChange(updatedSettings);

        // Assert: logging occurred
        mockLogger.Verify(log => log.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                MoqLogExtensions.MatchLogState("Configuration updated"),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }


    [Fact]
    public async Task ProcessEventOccurred_ShouldNotApplySettings_WhenProcessIsNotStarCraft()
    {
        _mockProcessEventWatcher.Raise(
            w => w.ProcessEventOccurred += null,
            new ProcessEventArgs(4688, 5678, "notepad.exe")
        );

        await Task.Delay(100);

        _mockLogger.Verify(
            log => log.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<object>(o => o.ToString()!.Contains("Applying key repeat settings")),
                null,
                It.IsAny<Func<object, Exception?, string>>()
            ), Times.Never);
    }

    [Fact]
    public async Task StartAsync_ShouldLogError_WhenProcessWatcherFailsToStart()
    {
        _mockProcessEventWatcher
            .Setup(w => w.Start())
            .Throws(new Exception("Failed to start watcher"));

        await _processMonitorService.StartAsync(CancellationToken.None);

        _mockLogger.Verify(log => log.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                MoqLogExtensions.MatchLogState("Failed to start process watcher."),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }


    [Fact]
    public void ProcessEventOccurred_ShouldIgnoreUnknownEventId_WhenInvalidEventIsReceived()
    {
        // Act
        _mockProcessEventWatcher.Raise(
            w => w.ProcessEventOccurred += null,
            new ProcessEventArgs(9999, 1234, "starcraft.exe")
        );

        // Assert
        _mockLogger.Verify(log => log.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                MoqLogExtensions.MatchLogState("Unrelated process event occurred: 9999 for PID 1234"),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessEventOccurred_ShouldHandleMultipleProcessStarts_WhenStarCraftStartsMultipleTimes()
    {
        // Arrange
        await _processMonitorService.StartAsync(CancellationToken.None);
        _mockLogger.Invocations.Clear();

        // Act
        _mockProcessEventWatcher.Raise(
            w => w.ProcessEventOccurred += null,
            new ProcessEventArgs(4688, 1234, "starcraft.exe")
        );
        _mockProcessEventWatcher.Raise(
            w => w.ProcessEventOccurred += null,
            new ProcessEventArgs(4688, 5678, "starcraft.exe")
        );

        // Assert
        _mockLogger.Verify(log => log.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                MoqLogExtensions.MatchLogState("Applying key repeat settings"),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }


    [Fact]
    public async Task ProcessEventOccurred_ShouldHandleMultipleProcessExits_WhenStarCraftStopsMultipleTimes()
    {
        // Arrange
        await _processMonitorService.StartAsync(CancellationToken.None);

        // Simulate two tracked process starts
        _mockProcessEventWatcher.Raise(
            w => w.ProcessEventOccurred += null,
            new ProcessEventArgs(4688, 1234, "starcraft.exe")
        );
        _mockProcessEventWatcher.Raise(
            w => w.ProcessEventOccurred += null,
            new ProcessEventArgs(4688, 5678, "starcraft.exe")
        );
        _mockLogger.Invocations.Clear(); // clear logs before simulating exits

        // Act: stop first process
        _mockProcessEventWatcher.Raise(
            w => w.ProcessEventOccurred += null,
            new ProcessEventArgs(4689, 1234, "starcraft.exe")
        );

        // Verify no "state changed to False" yet
        _mockLogger.Verify(log => log.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                MoqLogExtensions.MatchLogState("Process running state changed to False"),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);

        // Act: stop second (last) process
        _mockProcessEventWatcher.Raise(
            w => w.ProcessEventOccurred += null,
            new ProcessEventArgs(4689, 5678, "starcraft.exe")
        );

        // Final assertion that the state transitioned to False
        _mockLogger.Verify(log => log.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                MoqLogExtensions.MatchLogState("Process running state changed to False"),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}