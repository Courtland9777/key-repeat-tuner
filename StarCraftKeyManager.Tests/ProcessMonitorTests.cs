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
        await cts.CancelAsync();
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
            Times.Once);
    }

    [Fact]
    public void ProcessMonitor_ShouldHandleNullAppSettings()
    {
        // Arrange
        _mockOptionsMonitor.Setup(m => m.CurrentValue).Returns((AppSettings?)null!);
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new ProcessMonitorService(_mockLogger.Object, _mockOptionsMonitor.Object, _mockProcessEventWatcher.Object));
    }

    [Fact]
    public void ProcessMonitor_ShouldHandleMultipleProcessEvents()
    {
        // Arrange
        var events = new[]
        {
            new ProcessEventArgs(4688, 1234, "notepad.exe"),
            new ProcessEventArgs(4689, 1234, "notepad.exe"),
            new ProcessEventArgs(4688, 5678, "calc.exe")
        };
        // Act
        foreach (var processEvent in events)
            _mockProcessEventWatcher.Raise(m => m.ProcessEventOccurred += null, processEvent);
        // Assert
        _mockLogger.Verify(
            log => log.LogInformation("Process event occurred: {EventId} for PID {ProcessId}", It.IsAny<object[]>()),
            Times.Never);
    }

    [Fact]
    public void ProcessMonitor_ShouldIgnoreMismatchedProcessNames()
    {
        // Arrange
        var mismatchedEvent = new ProcessEventArgs(4688, 1234, "otherprocess.exe");
        // Act
        _mockProcessEventWatcher.Raise(m => m.ProcessEventOccurred += null, mismatchedEvent);
        // Assert
        _mockLogger.Verify(
            log => log.LogInformation("Process event occurred: {EventId} for PID {ProcessId}", It.IsAny<object[]>()),
            Times.Never);
    }

    [Fact]
    public async Task ProcessMonitor_ShouldStopServiceGracefully()
    {
        // Arrange
        var service = new ProcessMonitorService(_mockLogger.Object, _mockOptionsMonitor.Object,
            _mockProcessEventWatcher.Object);
        var cts = new CancellationTokenSource();
        // Act
        await service.StartAsync(cts.Token);
        await service.StopAsync(cts.Token);
        // Assert
        _mockLogger.Verify(log => log.LogInformation("Stopping process monitor service."), Times.Once);
    }

    [Fact]
    public async Task ProcessMonitor_ShouldLogAllMessagesOnStop()
    {
        // Arrange
        var service = new ProcessMonitorService(_mockLogger.Object, _mockOptionsMonitor.Object,
            _mockProcessEventWatcher.Object);
        var cts = new CancellationTokenSource();
        // Act
        await service.StartAsync(cts.Token);
        await service.StopAsync(cts.Token);
        // Assert
        _mockLogger.Verify(log => log.LogInformation("Stopping process monitor service."), Times.Once);
        _mockLogger.Verify(log => log.LogInformation("Process monitor service stopped."), Times.Once);
    }

    [Fact]
    public void ProcessMonitor_ShouldHandleConcurrentEvents()
    {
        // Arrange
        var events = new[]
        {
            new ProcessEventArgs(4688, 1234, "notepad.exe"),
            new ProcessEventArgs(4689, 1234, "notepad.exe"),
            new ProcessEventArgs(4688, 5678, "calc.exe")
        };
        // Act
        Parallel.ForEach(events,
            processEvent => { _mockProcessEventWatcher.Raise(m => m.ProcessEventOccurred += null, processEvent); });
        // Assert
        _mockLogger.Verify(
            log => log.LogInformation("Process event occurred: {EventId} for PID {ProcessId}", It.IsAny<object[]>()),
            Times.Exactly(events.Length));
    }

    [Fact]
    public void ProcessMonitor_ShouldLogWarningForInvalidEvents()
    {
        // Arrange
        var invalidEvent = new ProcessEventArgs(9999, 1234, "unknown.exe"); // Invalid EventId
        // Act
        _mockProcessEventWatcher.Raise(m => m.ProcessEventOccurred += null, invalidEvent);
        // Assert
        _mockLogger.Verify(
            log => log.LogWarning("Unexpected process event: {EventId} for PID {ProcessId}", 9999, 1234),
            Times.Once);
    }

    [Fact]
    public void ProcessMonitor_ShouldHandleIncompleteAppSettings()
    {
        // Arrange
        var incompleteSettings = new AppSettings
        {
            ProcessMonitor = null!,
            KeyRepeat = null!
        };
        _mockOptionsMonitor.Setup(m => m.CurrentValue).Returns(incompleteSettings);
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new ProcessMonitorService(_mockLogger.Object, _mockOptionsMonitor.Object, _mockProcessEventWatcher.Object));
    }

    [Fact]
    public void ProcessMonitor_ShouldApplyKeyRepeatSettings_WhenProcessStarts()
    {
        // Arrange
        var mockMonitorService = new Mock<ProcessMonitorService>(
            _mockLogger.Object, _mockOptionsMonitor.Object, _mockProcessEventWatcher.Object
        );

        _mockProcessEventWatcher.Raise(
            m => m.ProcessEventOccurred += null,
            new ProcessEventArgs(4688, 1234, "notepad.exe")
        );

        // Assert
        mockMonitorService.Verify(m => m.ApplyKeyRepeatSettings(), Times.Once);
    }

    [Fact]
    public async Task ProcessMonitor_ShouldHandleInvalidKeyRepeatSettings()
    {
        // Arrange
        var invalidSettings = new AppSettings
        {
            ProcessMonitor = new ProcessMonitorSettings { ProcessName = "notepad.exe" },
            KeyRepeat = new KeyRepeatSettings
            {
                Default = new KeyRepeatState { RepeatSpeed = -1, RepeatDelay = 0 }, // Invalid values
                FastMode = new KeyRepeatState { RepeatSpeed = -10, RepeatDelay = -500 } // Invalid values
            }
        };
        _mockOptionsMonitor.Setup(m => m.CurrentValue).Returns(invalidSettings);
        var service = new ProcessMonitorService(_mockLogger.Object, _mockOptionsMonitor.Object,
            _mockProcessEventWatcher.Object);
        var cts = new CancellationTokenSource();
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => service.StartAsync(cts.Token));
    }
}