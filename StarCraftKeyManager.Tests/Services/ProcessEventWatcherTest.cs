using System.Reflection;
using Microsoft.Extensions.Logging;
using Moq;
using StarCraftKeyManager.Services;
using StarCraftKeyManager.SystemAdapters.Interfaces;
using StarCraftKeyManager.Tests.TestUtilities.Extensions;
using Xunit;

namespace StarCraftKeyManager.Tests.Services;

public class ProcessEventWatcherTest
{
    private readonly Mock<ILogger<ProcessEventWatcher>> _mockLogger;
    private readonly Mock<IManagementEventWatcher> _mockStartWatcher;
    private readonly Mock<IManagementEventWatcher> _mockStopWatcher;
    private readonly ProcessEventWatcher _processEventWatcher;

    public ProcessEventWatcherTest()
    {
        _mockLogger = new Mock<ILogger<ProcessEventWatcher>>();
        var mockFactory = new Mock<IManagementEventWatcherFactory>();
        _mockStartWatcher = new Mock<IManagementEventWatcher>();
        _mockStopWatcher = new Mock<IManagementEventWatcher>();

        mockFactory.SetupSequence(f => f.Create(It.IsAny<string>()))
            .Returns(_mockStartWatcher.Object)
            .Returns(_mockStopWatcher.Object);

        _processEventWatcher = new ProcessEventWatcher(
            _mockLogger.Object,
            mockFactory.Object
        );
    }

    [Fact]
    public void Configure_ShouldSanitizeProcessName()
    {
        _processEventWatcher.Configure("test.exe");

        var name = GetPrivateField<string>(_processEventWatcher, "_processName");
        Assert.Equal("test", name);
    }

    [Fact]
    public void Configure_ShouldNotReconfigureIfSameProcessName()
    {
        _processEventWatcher.Configure("test.exe");
        _processEventWatcher.Configure("test.exe");

        _mockLogger.Verify(log => log.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            MoqLogExtensions.MatchLogState("Reconfiguring WMI process watcher"),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact]
    public void Start_ShouldInitializeWatchersOnce()
    {
        _processEventWatcher.Configure("test.exe");
        _processEventWatcher.Start();
        _processEventWatcher.Start();

        _mockStartWatcher.Verify(w => w.Start(), Times.Once);
        _mockStopWatcher.Verify(w => w.Start(), Times.Once);
    }

    [Fact]
    public void Stop_ShouldDisposeWatchers()
    {
        _processEventWatcher.Configure("test.exe");
        _processEventWatcher.Start();
        _processEventWatcher.Stop();

        _mockStartWatcher.Verify(w => w.Stop(), Times.Once);
        _mockStartWatcher.Verify(w => w.Dispose(), Times.Once);
        _mockStopWatcher.Verify(w => w.Stop(), Times.Once);
        _mockStopWatcher.Verify(w => w.Dispose(), Times.Once);
    }

    [Fact]
    public void Dispose_ShouldCallStop()
    {
        _processEventWatcher.Configure("test.exe");
        _processEventWatcher.Start();
        _processEventWatcher.Dispose();

        _mockStartWatcher.Verify(w => w.Stop(), Times.Once);
        _mockStopWatcher.Verify(w => w.Stop(), Times.Once);
    }

    [Fact]
    public void ProcessEventOccurred_ShouldTriggerOnStartEvent()
    {
        var triggered = false;
        var mockArgs = new Mock<IEventArrivedEventArgs>();
        mockArgs.Setup(x => x.GetProcessId()).Returns(1234);

        _processEventWatcher.Configure("test.exe");

        _processEventWatcher.ProcessEventOccurred += (_, args) =>
        {
            triggered = true;
            Assert.Equal(4688, args.EventId);
            Assert.Equal(1234, args.ProcessId);
            Assert.Equal("test.exe", args.ProcessName);
        };

        InvokePrivateMethod(_processEventWatcher, "HandleStartEvent", mockArgs.Object);

        Assert.True(triggered);
    }

    [Fact]
    public void ProcessEventOccurred_ShouldTriggerOnStopEvent()
    {
        var triggered = false;
        var mockArgs = new Mock<IEventArrivedEventArgs>();
        mockArgs.Setup(x => x.GetProcessId()).Returns(5678);

        _processEventWatcher.Configure("test.exe");

        _processEventWatcher.ProcessEventOccurred += (_, args) =>
        {
            triggered = true;
            Assert.Equal(4689, args.EventId);
            Assert.Equal(5678, args.ProcessId);
            Assert.Equal("test.exe", args.ProcessName);
        };

        InvokePrivateMethod(_processEventWatcher, "HandleStopEvent", mockArgs.Object);

        Assert.True(triggered);
    }

    private static T? GetPrivateField<T>(object obj, string fieldName)
    {
        var field = obj.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        return field != null ? (T?)field.GetValue(obj) : default;
    }

    private static void InvokePrivateMethod(object obj, string methodName, object param)
    {
        var method = obj.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
        method?.Invoke(obj, [param]);
    }
}