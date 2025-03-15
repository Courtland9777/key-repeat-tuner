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
    public async Task ProcessMonitor_ShouldRespectCancellationToken()
    {
        var service = new ProcessMonitorService(
            _mockLogger.Object,
            _mockOptionsMonitor.Object,
            _mockProcessEventWatcher.Object);
        var cts = new CancellationTokenSource();

        var task = service.StartAsync(cts.Token);
        cts.Cancel();
        await task;

        _mockLogger.Verify(log => log.LogInformation("Process monitor service cancellation requested."), Times.Once);
    }

    [Fact]
    public async Task ProcessMonitor_ShouldInitializeTrackedProcesses()
    {
        _mockProcessEventWatcher.Setup(m => m.Start());

        var service = new ProcessMonitorService(
            _mockLogger.Object,
            _mockOptionsMonitor.Object,
            _mockProcessEventWatcher.Object);
        var cts = new CancellationTokenSource();

        await service.StartAsync(cts.Token);

        _mockProcessEventWatcher.Verify(m => m.Start(), Times.Once);
        _mockLogger.Verify(log => log.LogInformation("Starting process monitor service."), Times.Once);
    }

    [Fact]
    public void ProcessMonitor_ShouldHandleProcessStartEvent()
    {
        _mockProcessEventWatcher.Raise(m => m.ProcessEventOccurred += null,
            new ProcessEventArgs(4688, 1234, "notepad.exe"));

        _mockLogger.Verify(log => log.LogInformation(
            "Process event occurred: {EventId} for PID {ProcessId}", 4688, 1234), Times.Once);
    }

    [Fact]
    public void ProcessMonitor_ShouldHandleProcessStopEvent()
    {
        _mockProcessEventWatcher.Raise(m => m.ProcessEventOccurred += null,
            new ProcessEventArgs(4688, 1234, "notepad.exe"));
        _mockProcessEventWatcher.Raise(m => m.ProcessEventOccurred += null,
            new ProcessEventArgs(4689, 1234, "notepad.exe"));

        _mockLogger.Verify(log => log.LogInformation(
            "Process event occurred: {EventId} for PID {ProcessId}", 4689, 1234), Times.Once);
    }

    [Fact]
    public async Task ProcessMonitor_ShouldHandleExceptionsGracefullyAsync()
    {
        _mockProcessEventWatcher.Setup(m => m.Start()).Throws(new Exception("Simulated failure"));

        var service = new ProcessMonitorService(
            _mockLogger.Object,
            _mockOptionsMonitor.Object,
            _mockProcessEventWatcher.Object);
        var cts = new CancellationTokenSource();

        var exception = await Record.ExceptionAsync(() => service.StartAsync(cts.Token));

        Assert.Null(exception);
        _mockLogger.Verify(log => log.LogError(It.IsAny<Exception>(), "Failed to start ProcessMonitorService"),
            Times.Never);
    }
}