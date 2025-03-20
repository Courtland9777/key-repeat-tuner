using System.Diagnostics.Eventing.Reader;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using StarCraftKeyManager.Models;
using StarCraftKeyManager.Services;

namespace StarCraftKeyManager.Tests.Integration;

public class ProcessEventWatcherIntegrationTests
{
    private readonly Mock<ILogger<ProcessEventWatcher>> _mockLogger;
    private readonly ProcessEventWatcher _processEventWatcher;

    public ProcessEventWatcherIntegrationTests()
    {
        _mockLogger = new Mock<ILogger<ProcessEventWatcher>>();
        var mockOptionsMonitor = new Mock<IOptionsMonitor<AppSettings>>();

        var mockSettings = new AppSettings
        {
            ProcessMonitor = new ProcessMonitorSettings { ProcessName = "starcraft.exe" },
            KeyRepeat = new KeyRepeatSettings
            {
                Default = new KeyRepeatState { RepeatSpeed = 31, RepeatDelay = 1000 },
                FastMode = new KeyRepeatState { RepeatSpeed = 20, RepeatDelay = 500 }
            }
        };

        mockOptionsMonitor.Setup(o => o.CurrentValue).Returns(mockSettings);

        _processEventWatcher = new ProcessEventWatcher(
            _mockLogger.Object,
            mockOptionsMonitor.Object
        );
    }

    [Fact]
    public void Configure_ShouldSetProcessName_WhenCalled()
    {
        // Act
        _processEventWatcher.Configure("starcraft.exe");

        // Assert
        _mockLogger.Verify(log => log.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<object>(o => o.ToString()!.Contains("Configured process watcher for starcraft.exe")),
            null,
            It.IsAny<Func<object, Exception?, string>>()
        ), Times.Once);
    }

    [Fact]
    public void Start_ShouldEnableEventWatcher_WhenInvoked()
    {
        // Act
        _processEventWatcher.Start();

        // Assert
        _mockLogger.Verify(log => log.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<object>(o => o.ToString()!.Contains("Process event watcher started")),
            null,
            It.IsAny<Func<object, Exception?, string>>()
        ), Times.Once);
    }

    [Fact]
    public void Stop_ShouldDisableEventWatcher_WhenInvoked()
    {
        // Arrange
        _processEventWatcher.Start();

        // Act
        _processEventWatcher.Stop();

        // Assert
        _mockLogger.Verify(log => log.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<object>(o => o.ToString()!.Contains("Process event watcher stopped")),
            null,
            It.IsAny<Func<object, Exception?, string>>()
        ), Times.Once);
    }

    [Fact]
    public void ProcessEventOccurred_ShouldTriggerEvent_WhenValidProcessStarts()
    {
        // Arrange
        var eventRaised = false;
        _processEventWatcher.ProcessEventOccurred += (_, _) => eventRaised = true;

        var mockEventArgs = CreateMockEventArgs(4688, "starcraft.exe");

        // Act
        _processEventWatcher.EventWatcherOnEventRecordWritten(this, mockEventArgs);

        // Assert
        Assert.True(eventRaised, "Expected ProcessEventOccurred to be raised for event ID 4688.");
    }

    [Fact]
    public void ProcessEventOccurred_ShouldTriggerEvent_WhenValidProcessStops()
    {
        // Arrange
        var eventRaised = false;
        _processEventWatcher.ProcessEventOccurred += (_, _) => eventRaised = true;

        var mockEventArgs = CreateMockEventArgs(4689, "starcraft.exe");

        // Act
        _processEventWatcher.EventWatcherOnEventRecordWritten(this, mockEventArgs);

        // Assert
        Assert.True(eventRaised, "Expected ProcessEventOccurred to be raised for event ID 4689.");
    }

    [Fact]
    public void ProcessEventOccurred_ShouldIgnoreUnknownEventIds()
    {
        // Arrange
        var eventRaised = false;
        _processEventWatcher.ProcessEventOccurred += (_, _) => eventRaised = true;

        var mockEventArgs = CreateMockEventArgs(9999, "starcraft.exe");

        // Act
        _processEventWatcher.EventWatcherOnEventRecordWritten(this, mockEventArgs);

        // Assert
        Assert.False(eventRaised, "ProcessEventOccurred should not trigger for unknown event IDs.");
    }

    [Fact]
    public void EventWatcherOnEventRecordWritten_ShouldHandleNullEventRecordGracefully()
    {
        // Arrange
        var mockEventArgs = new Mock<EventRecordWrittenEventArgs>();
        mockEventArgs.Setup(e => e.EventRecord).Returns((EventRecord?)null!);

        // Act
        _processEventWatcher.EventWatcherOnEventRecordWritten(this, mockEventArgs.Object);

        // Assert
        _mockLogger.Verify(
            log => log.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<object>(msg => msg.ToString()!.Contains("Null EventRecord received")),
                null,
                It.IsAny<Func<object, Exception?, string>>()
            ),
            Times.Once,
            "Expected a warning log when EventRecord is null."
        );
    }

    [Fact]
    public void EventWatcherOnEventRecordWritten_ShouldIgnoreEvents_WithMissingProperties()
    {
        // Arrange
        var mockEventArgs = CreateMockEventArgs(4688, null);

        // Act
        _processEventWatcher.EventWatcherOnEventRecordWritten(this, mockEventArgs);

        // Assert
        _mockLogger.Verify(
            log => log.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<object>(msg => msg.ToString()!.Contains("Missing required properties")),
                null,
                It.IsAny<Func<object, Exception?, string>>()
            ),
            Times.Once,
            "Expected a warning log when event properties are missing."
        );
    }

    [Fact]
    public async Task ProcessEventOccurred_ShouldProcessMultipleEvents_Correctly()
    {
        // Arrange
        var eventCount = 0;
        _processEventWatcher.ProcessEventOccurred += (_, _) => eventCount++;

        var mockEvents = new[]
        {
            CreateMockEventArgs(4688, "notepad.exe"),
            CreateMockEventArgs(4688, "chrome.exe"),
            CreateMockEventArgs(4689, "notepad.exe"),
            CreateMockEventArgs(4689, "chrome.exe"),
            CreateMockEventArgs(4688, "word.exe")
        };

        // Act
        await Task.Run(() =>
        {
            foreach (var mockEvent in mockEvents)
                _processEventWatcher.EventWatcherOnEventRecordWritten(this, mockEvent);
        });

        // Assert
        Assert.Equal(5, eventCount);
    }

    private static EventRecordWrittenEventArgs CreateMockEventArgs(int eventId, string? processName)
    {
        var mockEvent = new Mock<EventRecordWrittenEventArgs>();
        var mockRecord = new Mock<EventRecord>();

        mockRecord.Setup(r => r.Id).Returns(eventId);

        if (processName != null)
        {
            var eventProperties = new List<EventProperty>
            {
                CreateEventProperty(processName)
            };

            mockRecord.Setup(r => r.Properties).Returns(eventProperties);
        }
        else
        {
            mockRecord.Setup(r => r.Properties).Returns([]);
        }

        mockEvent.Setup(e => e.EventRecord).Returns(mockRecord.Object);

        return mockEvent.Object;
    }

    private static EventProperty CreateEventProperty(object value)
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