using System.Diagnostics.Eventing.Reader;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using StarCraftKeyManager.Interfaces;
using StarCraftKeyManager.Models;
using StarCraftKeyManager.Services;
using StarCraftKeyManager.Tests.TestHelpers;
using Xunit;

namespace StarCraftKeyManager.Tests;

public class ProcessEventWatcherTests
{
    private readonly Mock<ILogger<ProcessEventWatcher>> _mockLogger = new();
    private readonly Mock<IOptionsMonitor<AppSettings>> _mockOptionsMonitor = new();
    private readonly Mock<IEventLogQueryBuilder> _mockQueryBuilder = new();
    private readonly Mock<IEventWatcherFactory> _mockWatcherFactory = new();
    private readonly ProcessEventWatcher _watcher;

    public ProcessEventWatcherTests()
    {
        _mockOptionsMonitor.Setup(o => o.CurrentValue).Returns(new AppSettings
        {
            ProcessMonitor = new ProcessMonitorSettings { ProcessName = "starcraft.exe" },
            KeyRepeat = new KeyRepeatSettings
            {
                Default = new KeyRepeatState { RepeatSpeed = 31, RepeatDelay = 1000 },
                FastMode = new KeyRepeatState { RepeatSpeed = 20, RepeatDelay = 500 }
            }
        });

        var mockWrappedWatcher = new Mock<IWrappedEventLogWatcher>();

        _mockWatcherFactory
            .Setup(f => f.Create(It.IsAny<EventLogQuery>()))
            .Returns(mockWrappedWatcher.Object);

        var testQuery = new EventLogQuery("Security", PathType.LogName);
        _mockQueryBuilder.Setup(q => q.BuildQuery()).Returns(testQuery);

        _watcher = new ProcessEventWatcher(
            _mockLogger.Object,
            _mockOptionsMonitor.Object,
            _mockWatcherFactory.Object,
            _mockQueryBuilder.Object);

        _watcher.Configure("starcraft.exe");
    }

    [Fact]
    public void Start_And_Stop_ShouldLogCorrectly()
    {
        _watcher.Start();
        _watcher.Stop();

        _mockLogger.Verify(l => l.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("started")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);

        _mockLogger.Verify(l => l.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("stopped")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact]
    public void Event_ShouldBeRaised_ForValidStartAndStopEvents()
    {
        var raised = 0;
        _watcher.ProcessEventOccurred += (_, _) => raised++;

        var startArgs = FakeEventRecordFactory.WrapStartEvent("starcraft.exe");
        var stopArgs = FakeEventRecordFactory.WrapStopEvent("starcraft.exe");

        _watcher.EventWatcherOnEventRecordWritten(this, startArgs);
        _watcher.EventWatcherOnEventRecordWritten(this, stopArgs);

        Assert.Equal(2, raised);
    }


    [Fact]
    public void Event_ShouldNotBeRaised_ForUnknownEventId()
    {
        var raised = false;
        _watcher.ProcessEventOccurred += (_, _) => raised = true;

        _watcher.EventWatcherOnEventRecordWritten(this, CreateMockArgs(9999, 123));

        Assert.False(raised);
    }

    [Fact]
    public void ShouldLogWarning_WhenEventRecordIsNull()
    {
        var mockArgs = new Mock<EventRecordWrittenEventArgs>();
        mockArgs.Setup(a => a.EventRecord).Returns((EventRecord?)null!);

        _watcher.EventWatcherOnEventRecordWritten(this, mockArgs.Object);

        _mockLogger.Verify(l => l.Log(
            LogLevel.Warning,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("Null EventRecord")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact]
    public void ShouldLogError_WhenProcessIdExtractionFails()
    {
        var mockRecord = new Mock<EventRecord>();
        mockRecord.Setup(r => r.Id).Returns(4688);
        mockRecord.Setup(r => r.Properties).Returns([]); // Missing required indexes

        var mockArgs = new Mock<EventRecordWrittenEventArgs>();
        mockArgs.Setup(e => e.EventRecord).Returns(mockRecord.Object);

        _watcher.EventWatcherOnEventRecordWritten(this, mockArgs.Object);

        _mockLogger.Verify(l => l.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("Error extracting process ID")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact]
    public async Task MultipleEvents_ShouldAllTriggerEvent()
    {
        var count = 0;
        _watcher.ProcessEventOccurred += (_, _) => count++;

        IEnumerable<EventRecordWrittenEventArgs> events =
        [
            CreateMockArgs(4688, 1),
            CreateMockArgs(4688, 2),
            CreateMockArgs(4689, 1),
            CreateMockArgs(4689, 2),
            CreateMockArgs(4688, 3)
        ];

        await Task.Run(() =>
        {
            foreach (var e in events)
                _watcher.EventWatcherOnEventRecordWritten(this, e);
        });

        Assert.Equal(5, count);
    }

    private static EventRecordWrittenEventArgs CreateMockArgs(int eventId, int? processId)
    {
        var mockRecord = new Mock<EventRecord>();
        mockRecord.Setup(r => r.Id).Returns(eventId);

        mockRecord.Setup(r => r.Properties).Returns(processId is not null
            ?
            [
                CreateEventProp(0),
                CreateEventProp(processId.Value),
                CreateEventProp("some.exe")
            ]
            : []);

        var mockArgs = new Mock<EventRecordWrittenEventArgs>();
        mockArgs.Setup(a => a.EventRecord).Returns(mockRecord.Object);
        return mockArgs.Object;
    }

    private static EventProperty CreateEventProp(object value)
    {
        return (EventProperty)Activator.CreateInstance(
            typeof(EventProperty),
            BindingFlags.NonPublic | BindingFlags.Instance,
            null,
            [value],
            null
        )!;
    }
}