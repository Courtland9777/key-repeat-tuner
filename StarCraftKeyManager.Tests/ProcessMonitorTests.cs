using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using StarCraftKeyManager.Interfaces;
using StarCraftKeyManager.Models;
using StarCraftKeyManager.Services;

namespace StarCraftKeyManager.Tests;

public class ProcessMonitorTests
{
    private readonly Mock<ILogger<ProcessMonitorService>> _mockLogger = new();
    private readonly Mock<IOptionsMonitor<AppSettings>> _mockOptionsMonitor = new();
    private readonly Mock<IProcessEventWatcher> _mockProcessEventWatcher = new();

    public ProcessMonitorTests()
    {
        var appSettings = new AppSettings
        {
            ProcessMonitor = new ProcessMonitorSettings { ProcessName = "notepad.exe" },
            KeyRepeat = new KeyRepeatSettings
            {
                Default = new KeyRepeatState { RepeatSpeed = 31, RepeatDelay = 1000 },
                FastMode = new KeyRepeatState { RepeatSpeed = 20, RepeatDelay = 500 }
            }
        };

        _mockOptionsMonitor.Setup(m => m.CurrentValue).Returns(appSettings);
    }

    [Fact]
    public async Task ProcessMonitor_ShouldApplyInitialSettings_OnStart()
    {
        var service = new ProcessMonitorService(
            _mockLogger.Object,
            _mockOptionsMonitor.Object,
            _mockProcessEventWatcher.Object);
        var cts = new CancellationTokenSource();

        await service.StartAsync(cts.Token);

        _mockProcessEventWatcher.Verify(m => m.Start(), Times.Once);

        _mockLogger.Verify(log => log.LogInformation(
                "Applying key repeat settings: {@Settings}",
                It.Is<KeyRepeatState>(state => state.RepeatSpeed == 31 && state.RepeatDelay == 1000)),
            Times.Once);

        await service.StopAsync(cts.Token);

        _mockProcessEventWatcher.Verify(m => m.Stop(), Times.Once);
    }


    [Fact]
    public void ProcessMonitor_ShouldHandleProcessStartEvent()
    {
        _mockProcessEventWatcher.Raise(m => m.ProcessEventOccurred += null,
            new ProcessEventArgs(4688, 1234, "notepad.exe"));

        _mockLogger.Verify(log => log.LogInformation(
            "Process event occurred: {EventId} for PID {ProcessId}", 4688, 1234), Times.Once);

        _mockLogger.Verify(log => log.LogInformation(
                "Applying key repeat settings: {@Settings}",
                It.Is<KeyRepeatState>(state => state.RepeatSpeed == 20 && state.RepeatDelay == 500)),
            Times.Once);
    }

    [Fact]
    public void ProcessMonitor_ShouldHandleProcessStopEvent()
    {
        // Simulate process start
        _mockProcessEventWatcher.Raise(m => m.ProcessEventOccurred += null,
            new ProcessEventArgs(4688, 1234, "notepad.exe"));

        // Simulate process stop
        _mockProcessEventWatcher.Raise(m => m.ProcessEventOccurred += null,
            new ProcessEventArgs(4689, 1234, "notepad.exe"));

        _mockLogger.Verify(log => log.LogInformation(
            "Process event occurred: {EventId} for PID {ProcessId}",
            4689, 1234), Times.Once);

        _mockLogger.Verify(log => log.LogInformation(
                "Applying key repeat settings: {@Settings}",
                It.Is<KeyRepeatState>(state => state.RepeatSpeed == 31 && state.RepeatDelay == 1000)),
            Times.Once);
    }

    [Fact]
    public void ProcessMonitor_ShouldUpdateConfiguration_OnOptionsChange()
    {
        // Arrange
        Action<AppSettings, string?>? capturedOnChange = null;

        _mockOptionsMonitor
            .Setup(m => m.OnChange(It.IsAny<Action<AppSettings, string?>>()))
            .Callback<Action<AppSettings, string?>>(callback => capturedOnChange = callback);

        var newSettings = new AppSettings
        {
            ProcessMonitor = new ProcessMonitorSettings { ProcessName = "calc.exe" },
            KeyRepeat = new KeyRepeatSettings
            {
                Default = new KeyRepeatState { RepeatSpeed = 30, RepeatDelay = 750 },
                FastMode = new KeyRepeatState { RepeatSpeed = 15, RepeatDelay = 400 }
            }
        };

        // Act (invoke captured callback)
        capturedOnChange?.Invoke(newSettings, null);

        // Assert
        _mockProcessEventWatcher.Verify(m => m.Configure("calc.exe"), Times.Once);
        _mockLogger.Verify(log => log.LogInformation("Configuration updated: {@Settings}", newSettings), Times.Once);
    }
}