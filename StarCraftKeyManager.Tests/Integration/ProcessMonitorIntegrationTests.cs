using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using StarCraftKeyManager.Interfaces;
using StarCraftKeyManager.Models;
using StarCraftKeyManager.Services;

namespace StarCraftKeyManager.Tests.Integration;

public class ProcessMonitorIntegrationTests
{
    private readonly Mock<ILogger<ProcessMonitorService>> _mockLogger;
    private readonly Mock<IProcessEventWatcher> _mockProcessEventWatcher;
    private readonly ProcessMonitorService _processMonitorService;

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
    public async Task ProcessMonitor_ShouldStartAndApplyKeyRepeatSettings()
    {
        // Arrange
        var cts = new CancellationTokenSource();

        // Act
        var monitorTask = _processMonitorService.StartAsync(cts.Token);
        await Task.Delay(500); // Allow service to start

        // Assert
        _mockLogger.Verify(log => log.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<object>(o => o != null! && o.ToString()!.Contains("Process monitor service started")),
            null,
            It.IsAny<Func<object, Exception?, string>>()
        ), Times.Once);

        // Cleanup
        await cts.CancelAsync();
        await monitorTask;
    }

    [Fact]
    public async Task ProcessMonitor_ShouldDetectProcessStartAndApplyFastModeSettings()
    {
        // Arrange
        var tcs = new TaskCompletionSource<bool>();
        _mockProcessEventWatcher.Setup(w => w.Start()).Callback(() => tcs.SetResult(true));

        // Act
        await _processMonitorService.StartAsync(CancellationToken.None);
        await tcs.Task;
        _mockProcessEventWatcher.Raise(
            w => w.ProcessEventOccurred += null,
            new ProcessEventArgs(4688, 1234, "starcraft.exe")
        );

        // Assert
        _mockLogger.Verify(log => log.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<object>(o => o != null! && o.ToString()!.Contains("Applying key repeat settings: FastMode")),
            null,
            It.IsAny<Func<object, Exception?, string>>()
        ), Times.AtLeastOnce);
    }

    [Fact]
    public async Task ProcessMonitor_ShouldDetectProcessExitAndRestoreDefaultSettings()
    {
        // Arrange
        var tcs = new TaskCompletionSource<bool>();
        _mockProcessEventWatcher.Setup(w => w.Start()).Callback(() => tcs.SetResult(true));

        await _processMonitorService.StartAsync(CancellationToken.None);
        await tcs.Task;

        // Simulate process start
        _mockProcessEventWatcher.Raise(
            w => w.ProcessEventOccurred += null,
            new ProcessEventArgs(4688, 1234, "starcraft.exe")
        );
        await Task.Delay(500);

        // Simulate process exit
        _mockProcessEventWatcher.Raise(
            w => w.ProcessEventOccurred += null,
            new ProcessEventArgs(4689, 1234, "starcraft.exe")
        );

        // Assert
        _mockLogger.Verify(log => log.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<object>(o => o != null! && o.ToString()!.Contains("Restoring default key repeat settings")),
            null,
            It.IsAny<Func<object, Exception?, string>>()
        ), Times.AtLeastOnce);
    }
}