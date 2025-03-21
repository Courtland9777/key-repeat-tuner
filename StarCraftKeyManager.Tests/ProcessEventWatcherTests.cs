using System.Diagnostics.Eventing.Reader;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using StarCraftKeyManager.Models;
using StarCraftKeyManager.Services;
using Xunit;

namespace StarCraftKeyManager.Tests;

public class ProcessEventWatcherTests
{
    private readonly Mock<ILogger<ProcessEventWatcher>> _mockLogger;
    private readonly ProcessEventWatcher _processEventWatcher;

    public ProcessEventWatcherTests()
    {
        _mockLogger = new Mock<ILogger<ProcessEventWatcher>>();
        Mock<IOptionsMonitor<AppSettings>> mockOptionsMonitor = new();

        var mockAppSettings = new AppSettings
        {
            ProcessMonitor = new ProcessMonitorSettings
            {
                ProcessName = "starcraft.exe"
            },
            KeyRepeat = new KeyRepeatSettings
            {
                Default = new KeyRepeatState { RepeatSpeed = 31, RepeatDelay = 1000 },
                FastMode = new KeyRepeatState { RepeatSpeed = 20, RepeatDelay = 500 }
            }
        };

        mockOptionsMonitor.Setup(o => o.CurrentValue).Returns(mockAppSettings);
        _processEventWatcher = new ProcessEventWatcher(_mockLogger.Object, mockOptionsMonitor.Object);
    }

    [Fact]
    public void ProcessEventWatcher_ShouldStartAndStopWithoutErrors()
    {
        // Act
        _processEventWatcher.Start();
        _processEventWatcher.Stop();

        // Assert
        _mockLogger.VerifyNoOtherCalls(); // Ensure no unexpected logs
    }

    [Fact]
    public void ProcessEventWatcher_ShouldTriggerProcessEventOccurred_WithValidEvent()
    {
        // Arrange
        var eventRaised = false;
        _processEventWatcher.ProcessEventOccurred += (_, _) => eventRaised = true;

        var mockEventArgs = CreateMockEventArgs(4688, "notepad.exe");

        // Act
        _processEventWatcher.EventWatcherOnEventRecordWritten(this, mockEventArgs);

        // Assert
        Assert.True(eventRaised, "ProcessEventOccurred should have been raised for event ID 4688.");
    }

    [Fact]
    public void ProcessEventWatcher_ShouldHandleNullEventRecord_Gracefully()
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
                It.IsAny<Func<object, Exception?, string>>()),
            Times.Once,
            "Expected a warning log when EventRecord is null."
        );
    }

    [Fact]
    public void ProcessEventWatcher_ShouldIgnoreEvents_WithMissingProperties()
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
                It.IsAny<Func<object, Exception?, string>>()),
            Times.Once,
            "Expected a warning log when event properties are missing."
        );
    }

    [Fact]
    public async Task ProcessEventWatcher_ShouldProcessMultipleEvents_Correctly()
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

    [Fact]
    public void ProcessEventWatcher_ShouldIgnoreInvalidEventIds()
    {
        // Arrange
        var eventRaised = false;
        _processEventWatcher.ProcessEventOccurred += (_, _) => eventRaised = true;

        var mockEventArgs = CreateMockEventArgs(9999, "unknown.exe"); // Invalid event ID

        // Act
        _processEventWatcher.EventWatcherOnEventRecordWritten(this, mockEventArgs);

        // Assert
        Assert.False(eventRaised, "ProcessEventOccurred should not trigger for unknown event IDs.");
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