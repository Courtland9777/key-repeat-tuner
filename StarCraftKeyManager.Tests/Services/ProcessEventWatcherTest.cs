using System.Management;
using System.Reflection;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using StarCraftKeyManager.Configuration;
using StarCraftKeyManager.Services;
using StarCraftKeyManager.Tests.TestUtilities.Extensions;
using Xunit;

namespace StarCraftKeyManager.Tests.Services;

[TestSubject(typeof(ProcessEventWatcher))]
public class ProcessEventWatcherTest
{
    private readonly Mock<ILogger<ProcessEventWatcher>> _mockLogger;
    private readonly Mock<IOptionsMonitor<AppSettings>> _mockOptionsMonitor;
    private readonly ProcessEventWatcher _processEventWatcher;

    public ProcessEventWatcherTest()
    {
        _mockLogger = new Mock<ILogger<ProcessEventWatcher>>();
        _mockOptionsMonitor = new Mock<IOptionsMonitor<AppSettings>>();
        _processEventWatcher = new ProcessEventWatcher(_mockLogger.Object, _mockOptionsMonitor.Object);
    }

    [Fact]
    public void Configure_ShouldSanitizeProcessName()
    {
        // Arrange
        var processName = "test.exe";

        // Act
        _processEventWatcher.Configure(processName);

        // Assert
        Assert.Equal("test", GetPrivateField<string>(_processEventWatcher, "_processName"));
    }

    [Fact]
    public void Configure_ShouldNotReconfigureIfSameProcessName()
    {
        // Arrange
        var processName = "test.exe";
        _processEventWatcher.Configure(processName);

        // Act
        _processEventWatcher.Configure(processName);

        // Assert
        _mockLogger.Verify(
            log => log.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                MoqLogExtensions.MatchLogState("Reconfiguring WMI process watcher"),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()
            ),
            Times.Once
        );
    }

    [Fact]
    public void Start_ShouldInitializeAndStartWatchers()
    {
        // Arrange
        _processEventWatcher.Configure("test.exe");

        // Act
        _processEventWatcher.Start();

        // Assert
        Assert.NotNull(GetPrivateField<ManagementEventWatcher>(_processEventWatcher, "_startWatcher"));
        Assert.NotNull(GetPrivateField<ManagementEventWatcher>(_processEventWatcher, "_stopWatcher"));
    }

    [Fact]
    public void Start_ShouldNotStartIfAlreadyStarted()
    {
        // Arrange
        _processEventWatcher.Configure("test.exe");
        _processEventWatcher.Start();

        // Act
        _processEventWatcher.Start();

        // Assert
        _mockLogger.Verify(
            log => log.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                MoqLogExtensions.MatchLogState("WMI process watchers started."),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()
            ),
            Times.Once
        );
    }

    [Fact]
    public void Stop_ShouldDisposeWatchers()
    {
        // Arrange
        _processEventWatcher.Configure("test.exe");
        _processEventWatcher.Start();

        // Act
        _processEventWatcher.Stop();

        // Assert
        Assert.Null(GetPrivateField<ManagementEventWatcher>(_processEventWatcher, "_startWatcher"));
        Assert.Null(GetPrivateField<ManagementEventWatcher>(_processEventWatcher, "_stopWatcher"));
    }

    [Fact]
    public void Stop_ShouldNotThrowIfWatchersAreNull()
    {
        // Act
        var exception = Record.Exception(() => _processEventWatcher.Stop());

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public void Dispose_ShouldCallStop()
    {
        // Arrange
        var mockWatcher = new Mock<ProcessEventWatcher>(_mockLogger.Object, _mockOptionsMonitor.Object)
            { CallBase = true };
        mockWatcher.Setup(x => x.Stop());

        // Act
        mockWatcher.Object.Dispose();

        // Assert
        mockWatcher.Verify(x => x.Stop(), Times.Once);
    }

    [Fact]
    public void ProcessEventOccurred_ShouldTriggerOnStartEvent()
    {
        // Arrange
        var eventTriggered = false;
        _processEventWatcher.ProcessEventOccurred += (_, args) =>
        {
            eventTriggered = true;
            Assert.Equal(4688, args.EventId);
            Assert.Equal(1234, args.ProcessId);
            Assert.Equal("test.exe", args.ProcessName);
        };

        _processEventWatcher.Configure("test.exe");
        _processEventWatcher.Start();

        var startWatcher = GetPrivateField<ManagementEventWatcher>(_processEventWatcher, "_startWatcher");
        var args = CreateFakeEventArgs(1234);

        // Act
        _processEventWatcher.GetType()
            .GetMethod("HandleStartEvent", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(_processEventWatcher, [args]);

        // Assert
        Assert.True(eventTriggered);
    }

    [Fact]
    public void ProcessEventOccurred_ShouldTriggerOnStopEvent()
    {
        // Arrange
        var eventTriggered = false;
        _processEventWatcher.ProcessEventOccurred += (_, args) =>
        {
            eventTriggered = true;
            Assert.Equal(4689, args.EventId);
            Assert.Equal(5678, args.ProcessId);
            Assert.Equal("test.exe", args.ProcessName);
        };

        _processEventWatcher.Configure("test.exe");
        _processEventWatcher.Start();

        var stopWatcher = GetPrivateField<ManagementEventWatcher>(_processEventWatcher, "_stopWatcher");
        var args = CreateFakeEventArgs(5678);


        // Act
        _processEventWatcher.GetType()
            .GetMethod("OnStopEventArrived", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(_processEventWatcher, [args]);

        // Assert
        Assert.True(eventTriggered);
    }

    private static T? GetPrivateField<T>(object obj, string fieldName)
    {
        var field = obj.GetType().GetField(fieldName,
            BindingFlags.NonPublic | BindingFlags.Instance);
        return field != null ? (T?)field.GetValue(obj) : default;
    }

    private static EventArrivedEventArgs CreateFakeEventArgs(int pid)
    {
        var wmiEvent = new ManagementClass("Win32_ProcessStartTrace").CreateInstance();
        wmiEvent["ProcessID"] = pid;

        return (EventArrivedEventArgs)Activator.CreateInstance(
            typeof(EventArrivedEventArgs),
            BindingFlags.Instance | BindingFlags.NonPublic,
            null,
            [wmiEvent],
            null
        )!;
    }
}