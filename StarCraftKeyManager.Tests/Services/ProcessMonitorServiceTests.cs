using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using StarCraftKeyManager.Configuration;
using StarCraftKeyManager.Events;
using StarCraftKeyManager.Interfaces;
using StarCraftKeyManager.Services;
using StarCraftKeyManager.SystemAdapters.Interfaces;
using StarCraftKeyManager.Tests.TestUtilities.Extensions;
using StarCraftKeyManager.Tests.TestUtilities.Stubs;
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
            MoqLogExtensions.MatchLogState("Applying key repeat settings: Mode=FastMode"),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact]
    public async Task StartAsync_ShouldDetectProcessMonitoring_WhenInvoked()
    {
        await _processMonitorService.StartAsync(CancellationToken.None);

        _mockLogger.Verify(log => log.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            MoqLogExtensions.MatchLogState("Monitoring process: starcraft.exe"),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact]
    public void ProcessEventOccurred_ShouldDetectProcessExit_AndRestoreDefaultSettings()
    {
        _mockProcessEventWatcher.Raise(
            w => w.ProcessEventOccurred += null,
            new ProcessEventArgs(4688, 1234, "starcraft.exe") // Start
        );

        _mockLogger.Invocations.Clear(); // Clean up logs before testing the transition

        _mockProcessEventWatcher.Raise(
            w => w.ProcessEventOccurred += null,
            new ProcessEventArgs(4689, 1234, "starcraft.exe") // Exit
        );


        _mockLogger.Verify(log => log.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            MoqLogExtensions.MatchLogState("Process running state changed to False"),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
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
            MoqLogExtensions.MatchLogState("Applying key repeat settings"),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Exactly(2));
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
            MoqLogExtensions.MatchLogState("Applying key repeat settings:"),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()
        ), Times.Exactly(2));
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
            MoqLogExtensions.MatchLogState("Process running state changed to False"),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
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
            MoqLogExtensions.MatchLogState("Starting process monitor service."),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Exactly(2));
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

    [Fact]
    public async Task Should_LogError_WhenProcessWatcherFailsToStart()
    {
        _mockProcessEventWatcher.Setup(p => p.Start()).Throws(new InvalidOperationException("fail"));

        await _processMonitorService.StartAsync(CancellationToken.None);

        _mockLogger.Verify(log => log.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            MoqLogExtensions.MatchLogState("Failed to start process watcher."),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact]
    public async Task Should_ApplyUpdatedSettings_WhenOptionsChange()
    {
        var optionsMonitor = new TestOptionsMonitor<AppSettings>(new AppSettings
        {
            ProcessMonitor = new ProcessMonitorSettings { ProcessName = "starcraft.exe" },
            KeyRepeat = new KeyRepeatSettings
            {
                Default = new KeyRepeatState { RepeatSpeed = 31, RepeatDelay = 1000 },
                FastMode = new KeyRepeatState { RepeatSpeed = 20, RepeatDelay = 500 }
            }
        });

        var service = new ProcessMonitorService(
            _mockLogger.Object,
            optionsMonitor,
            _mockProcessEventWatcher.Object,
            _mockKeyboardSettingsApplier.Object,
            _mockProcessProvider.Object
        );

        await service.StartAsync(CancellationToken.None);

        optionsMonitor.TriggerChange(new AppSettings
        {
            ProcessMonitor = new ProcessMonitorSettings { ProcessName = "newgame.exe" },
            KeyRepeat = new KeyRepeatSettings
            {
                Default = new KeyRepeatState { RepeatSpeed = 10, RepeatDelay = 300 },
                FastMode = new KeyRepeatState { RepeatSpeed = 5, RepeatDelay = 200 }
            }
        });

        _mockLogger.Verify(log => log.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            MoqLogExtensions.MatchLogState("Configuration updated"),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact]
    public async Task Should_ApplyFastMode_WhenOneProcessRemains()
    {
        await _processMonitorService.StartAsync(CancellationToken.None);

        _mockProcessEventWatcher.Raise(w => w.ProcessEventOccurred += null,
            new ProcessEventArgs(4688, 1111, "starcraft.exe"));
        _mockProcessEventWatcher.Raise(w => w.ProcessEventOccurred += null,
            new ProcessEventArgs(4688, 2222, "starcraft.exe"));
        _mockProcessEventWatcher.Raise(w => w.ProcessEventOccurred += null,
            new ProcessEventArgs(4689, 1111, "starcraft.exe")); // one exits

        // FastMode should still be active since one process remains
        _mockKeyboardSettingsApplier.Verify(apply =>
            apply.ApplyRepeatSettings(20, 500), Times.Once); // still in fast mode
    }

    [Fact]
    public async Task Should_ApplyDefault_WhenProcessNameIsChangedInConfig()
    {
        var optionsMonitor = new TestOptionsMonitor<AppSettings>(new AppSettings
        {
            ProcessMonitor = new ProcessMonitorSettings { ProcessName = "starcraft.exe" },
            KeyRepeat = new KeyRepeatSettings
            {
                Default = new KeyRepeatState { RepeatSpeed = 40, RepeatDelay = 700 },
                FastMode = new KeyRepeatState { RepeatSpeed = 20, RepeatDelay = 500 }
            }
        });

        var service = new ProcessMonitorService(
            _mockLogger.Object,
            optionsMonitor,
            _mockProcessEventWatcher.Object,
            _mockKeyboardSettingsApplier.Object,
            _mockProcessProvider.Object
        );

        await service.StartAsync(CancellationToken.None);

        _mockProcessEventWatcher.Raise(w => w.ProcessEventOccurred += null,
            new ProcessEventArgs(4688, 1000, "starcraft.exe"));

        optionsMonitor.TriggerChange(new AppSettings
        {
            ProcessMonitor = new ProcessMonitorSettings { ProcessName = "newgame.exe" },
            KeyRepeat = new KeyRepeatSettings
            {
                Default = new KeyRepeatState { RepeatSpeed = 40, RepeatDelay = 700 },
                FastMode = new KeyRepeatState { RepeatSpeed = 20, RepeatDelay = 500 }
            }
        });

        _mockKeyboardSettingsApplier.Verify(apply =>
            apply.ApplyRepeatSettings(40, 700), Times.AtLeast(1));
    }

    [Fact]
    public async Task Should_LogAndIgnore_UnrelatedEventIds()
    {
        await _processMonitorService.StartAsync(CancellationToken.None);

        _mockProcessEventWatcher.Raise(
            w => w.ProcessEventOccurred += null,
            new ProcessEventArgs(9999, 8888, "starcraft.exe")
        );

        _mockLogger.Verify(log => log.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            MoqLogExtensions.MatchLogState("Unrelated process event occurred: 9999 for PID 8888"),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact]
    public void ProcessEventOccurred_ShouldNotChangeState_WhenDuplicateStartOrStopReceived()
    {
        // Arrange
        _mockProcessEventWatcher.Raise(w => w.ProcessEventOccurred += null,
            new ProcessEventArgs(4688, 1234, "starcraft.exe")); // add
        _mockLogger.Invocations.Clear();

        // Act: Send duplicate start
        _mockProcessEventWatcher.Raise(w => w.ProcessEventOccurred += null,
            new ProcessEventArgs(4688, 1234, "starcraft.exe")); // no state change

        // Assert
        _mockLogger.Verify(l => l.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            MoqLogExtensions.MatchLogState("Process running state changed"),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Never);
    }
}