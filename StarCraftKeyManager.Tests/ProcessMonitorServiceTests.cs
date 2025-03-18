using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using StarCraftKeyManager.Interfaces;
using StarCraftKeyManager.Models;
using StarCraftKeyManager.Services;

namespace StarCraftKeyManager.Tests;

public class ProcessMonitorServiceTests
{
    private readonly Mock<ILogger<ProcessMonitorService>> _mockLogger;
    private readonly Mock<IProcessEventWatcher> _mockProcessEventWatcher;
    private readonly ProcessMonitorService _processMonitorService;

    public ProcessMonitorServiceTests()
    {
        _mockLogger = new Mock<ILogger<ProcessMonitorService>>();
        Mock<IOptionsMonitor<AppSettings>> mockOptionsMonitor = new();
        _mockProcessEventWatcher = new Mock<IProcessEventWatcher>();

        // Mock AppSettings
        var mockAppSettings = new AppSettings
        {
            ProcessMonitor = new ProcessMonitorSettings { ProcessName = "starcraft.exe" },
            KeyRepeat = new KeyRepeatSettings
            {
                Default = new KeyRepeatState { RepeatSpeed = 31, RepeatDelay = 1000 },
                FastMode = new KeyRepeatState { RepeatSpeed = 20, RepeatDelay = 500 }
            }
        };

        mockOptionsMonitor.Setup(o => o.CurrentValue).Returns(mockAppSettings);

        _processMonitorService = new ProcessMonitorService(
            _mockLogger.Object,
            mockOptionsMonitor.Object,
            _mockProcessEventWatcher.Object
        );
    }

    [Fact]
    public async Task ProcessMonitor_ShouldStartAndStopWithoutErrors()
    {
        // Act
        await _processMonitorService.StartAsync(CancellationToken.None);
        await _processMonitorService.StopAsync(CancellationToken.None);

        // Assert
        _mockLogger.VerifyNoOtherCalls(); // Ensure no unexpected logs
    }

    [Fact]
    public async Task ProcessMonitor_ShouldApplyKeyRepeatSettings_WhenProcessStarts()
    {
        // Arrange
        var tcs = new TaskCompletionSource<bool>();
        _mockProcessEventWatcher
            .Setup(watcher => watcher.Start())
            .Callback(() => tcs.SetResult(true));

        // Act
        await _processMonitorService.StartAsync(CancellationToken.None);
        await tcs.Task; // ✅ Ensures process event is fully handled

        // Assert
        _mockLogger.Verify(
            log => log.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<object>(o => o.ToString()!.Contains("Applying key repeat settings")),
                null,
                It.IsAny<Func<object, Exception?, string>>()
            ),
            Times.AtLeastOnce,
            "Expected key repeat settings to be applied on process start."
        );
    }

    [Fact]
    public async Task ProcessMonitor_ShouldTrackProcess_WhenStarted()
    {
        // Arrange
        var tcs = new TaskCompletionSource<bool>();
        _mockProcessEventWatcher.Setup(w => w.Start()).Callback(() => tcs.SetResult(true));

        // Act
        await _processMonitorService.StartAsync(CancellationToken.None);
        await tcs.Task;

        // Assert
        _mockLogger.Verify(
            log => log.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<object>(o => o.ToString()!.Contains("Monitoring process: starcraft.exe")),
                null,
                It.IsAny<Func<object, Exception?, string>>()
            ),
            Times.AtLeastOnce,
            "Expected process monitoring to start."
        );
    }

    [Fact]
    public async Task ProcessMonitor_ShouldDetectProcessExit()
    {
        // Arrange
        var tcs = new TaskCompletionSource<bool>();
        _mockProcessEventWatcher
            .Setup(w => w.Start())
            .Callback(() => _mockProcessEventWatcher.Raise(
                w => w.ProcessEventOccurred += null,
                new ProcessEventArgs(4689, 1234, "starcraft.exe")
            ));

        _mockProcessEventWatcher
            .Setup(w => w.Stop())
            .Callback(() => tcs.SetResult(true));

        // Act
        await _processMonitorService.StartAsync(CancellationToken.None);
        await tcs.Task;

        // Assert
        _mockLogger.Verify(
            log => log.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<object>(o => o.ToString()!.Contains("Process exited: starcraft.exe")),
                null,
                It.IsAny<Func<object, Exception?, string>>()
            ),
            Times.AtLeastOnce,
            "Expected process exit to be detected and logged."
        );
    }

    [Fact]
    public async Task ProcessMonitor_ShouldNotApplySettings_WhenProcessNameDoesNotMatch()
    {
        // Arrange
        _mockProcessEventWatcher.Raise(
            w => w.ProcessEventOccurred += null,
            new ProcessEventArgs(4688, 1234, "notepad.exe") // Different process
        );

        // Act
        await Task.Delay(100); // Give time for event processing

        // Assert
        _mockLogger.Verify(
            log => log.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<object>(o => o.ToString()!.Contains("Applying key repeat settings")),
                null,
                It.IsAny<Func<object, Exception?, string>>()
            ),
            Times.Never,
            "Expected no key repeat settings change when process name does not match."
        );
    }

    [Fact]
    public async Task ProcessMonitor_ShouldRestoreDefaultSettings_WhenProcessExits()
    {
        _mockProcessEventWatcher
            .Setup(w => w.Start())
            .Callback(() => _mockProcessEventWatcher.Raise(
                w => w.ProcessEventOccurred += null,
                new ProcessEventArgs(4689, 1234, "starcraft.exe") // Exit event
            ));

        await _processMonitorService.StartAsync(CancellationToken.None);

        _mockLogger.Verify(
            log => log.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<object>(msg => msg.ToString()!.Contains("Restoring default key repeat settings")),
                null,
                It.IsAny<Func<object, Exception?, string>>()
            ),
            Times.AtLeastOnce
        );
    }
}