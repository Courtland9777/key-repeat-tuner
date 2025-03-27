using Microsoft.Extensions.Logging;
using Moq;
using StarCraftKeyManager.Adapters;
using StarCraftKeyManager.Configuration;
using StarCraftKeyManager.Events;
using StarCraftKeyManager.Models;
using StarCraftKeyManager.Services;
using StarCraftKeyManager.Tests.TestUtilities.Extensions;
using StarCraftKeyManager.Tests.TestUtilities.Fakes;
using StarCraftKeyManager.Tests.TestUtilities.Stubs;
using Xunit;

namespace StarCraftKeyManager.Tests.Services.ProcessEventWatcherTests;

public class ProcessEventHandlingTests
{
    [Fact]
    public void ProcessEventOccurred_ShouldBeRaised_WhenStartEventReceived()
    {
        // Arrange
        var appSettings = new AppSettings
        {
            ProcessMonitor = new ProcessMonitorSettings { ProcessName = "starcraft.exe" },
            KeyRepeat = new KeyRepeatSettings
            {
                Default = new KeyRepeatState { RepeatSpeed = 31, RepeatDelay = 1000 },
                FastMode = new KeyRepeatState { RepeatSpeed = 20, RepeatDelay = 500 }
            }
        };

        var optionsMonitor = new TestOptionsMonitor<AppSettings>(appSettings);
        var logger = new Mock<ILogger<ProcessEventWatcher>>();
        var queryBuilder = new SecurityAuditQueryBuilder();
        var watcherFactory = new FakeWrappedWatcher();

        var watcher = new ProcessEventWatcher(
            logger.Object,
            optionsMonitor,
            watcherFactory,
            queryBuilder
        );

        watcher.Configure("starcraft.exe");

        ProcessEventArgs? capturedEvent = null;
        watcher.ProcessEventOccurred += (_, e) => capturedEvent = e;

        var fakeEvent = new FakeWrappedEventRecord(4688, 1234, "starcraft.exe");
        var args = new FakeWrappedEventRecordWrittenEventArgs(fakeEvent);

        // Act
        watcher.HandleEvent(args);

        // Assert
        Assert.NotNull(capturedEvent);
        Assert.Equal(4688, capturedEvent!.EventId);
        Assert.Equal(1234, capturedEvent.ProcessId);
        Assert.Equal("starcraft.exe", capturedEvent.ProcessName);
    }

    [Fact]
    public void ProcessEventOccurred_ShouldBeRaised_WhenStopEventReceived()
    {
        // Arrange
        var appSettings = new AppSettings
        {
            ProcessMonitor = new ProcessMonitorSettings { ProcessName = "starcraft.exe" },
            KeyRepeat = new KeyRepeatSettings
            {
                Default = new KeyRepeatState { RepeatSpeed = 31, RepeatDelay = 1000 },
                FastMode = new KeyRepeatState { RepeatSpeed = 20, RepeatDelay = 500 }
            }
        };

        var optionsMonitor = new TestOptionsMonitor<AppSettings>(appSettings);
        var logger = new Mock<ILogger<ProcessEventWatcher>>();
        var queryBuilder = new SecurityAuditQueryBuilder();
        var watcherFactory = new FakeWrappedWatcher();

        var watcher = new ProcessEventWatcher(
            logger.Object,
            optionsMonitor,
            watcherFactory,
            queryBuilder
        );

        watcher.Configure("starcraft.exe");

        ProcessEventArgs? capturedEvent = null;
        watcher.ProcessEventOccurred += (_, e) => capturedEvent = e;

        var fakeEvent = new FakeWrappedEventRecord(4689, 5678, "starcraft.exe");
        var args = new FakeWrappedEventRecordWrittenEventArgs(fakeEvent);

        // Act
        watcher.HandleEvent(args);

        // Assert
        Assert.NotNull(capturedEvent);
        Assert.Equal(4689, capturedEvent!.EventId);
        Assert.Equal(5678, capturedEvent.ProcessId);
        Assert.Equal("starcraft.exe", capturedEvent.ProcessName);
    }


    [Fact]
    public void ProcessEventOccurred_ShouldBeRaised_WhenUnknownEventId()
    {
        // Arrange
        var appSettings = new AppSettings
        {
            ProcessMonitor = new ProcessMonitorSettings { ProcessName = "starcraft.exe" },
            KeyRepeat = new KeyRepeatSettings
            {
                Default = new KeyRepeatState { RepeatSpeed = 31, RepeatDelay = 1000 },
                FastMode = new KeyRepeatState { RepeatSpeed = 20, RepeatDelay = 500 }
            }
        };

        var optionsMonitor = new TestOptionsMonitor<AppSettings>(appSettings);
        var logger = new Mock<ILogger<ProcessEventWatcher>>();
        var queryBuilder = new SecurityAuditQueryBuilder();
        var watcherFactory = new FakeWrappedWatcher();

        var watcher = new ProcessEventWatcher(
            logger.Object,
            optionsMonitor,
            watcherFactory,
            queryBuilder
        );

        watcher.Configure("starcraft.exe");

        ProcessEventArgs? raised = null;
        watcher.ProcessEventOccurred += (_, e) => raised = e;

        var unknownEvent = new FakeWrappedEventRecord(9999, 1111, "starcraft.exe");
        var args = new FakeWrappedEventRecordWrittenEventArgs(unknownEvent);

        // Act
        watcher.HandleEvent(args);

        // Assert
        Assert.NotNull(raised);
        Assert.Equal(9999, raised!.EventId);
        Assert.Equal(1111, raised.ProcessId);
        Assert.Equal("starcraft.exe", raised.ProcessName);
    }


    [Fact]
    public void ProcessEventOccurred_ShouldNotBeRaised_WhenProcessIdIsMissing()
    {
        // Arrange
        var appSettings = new AppSettings
        {
            ProcessMonitor = new ProcessMonitorSettings { ProcessName = "starcraft.exe" },
            KeyRepeat = new KeyRepeatSettings
            {
                Default = new KeyRepeatState { RepeatSpeed = 31, RepeatDelay = 1000 },
                FastMode = new KeyRepeatState { RepeatSpeed = 20, RepeatDelay = 500 }
            }
        };

        var optionsMonitor = new TestOptionsMonitor<AppSettings>(appSettings);
        var logger = new Mock<ILogger<ProcessEventWatcher>>();
        var queryBuilder = new SecurityAuditQueryBuilder();
        var watcherFactory = new FakeWrappedWatcher();

        var watcher = new ProcessEventWatcher(
            logger.Object,
            optionsMonitor,
            watcherFactory,
            queryBuilder
        );

        watcher.Configure("starcraft.exe");

        var wasRaised = false;
        watcher.ProcessEventOccurred += (_, _) => wasRaised = true;

        // 👇 simulate invalid process ID using the second constructor
        var brokenEvent = new FakeWrappedEventRecord(4688, "not-a-number", "starcraft.exe");
        var args = new FakeWrappedEventRecordWrittenEventArgs(brokenEvent);

        // Act
        watcher.HandleEvent(args);

        // Assert
        Assert.False(wasRaised, "Expected event NOT to be raised when process ID is invalid.");
    }


    [Fact]
    public void ProcessEventOccurred_ShouldBeRaised_MultipleTimes_WhenMultipleEventsReceived()
    {
        // Arrange
        var appSettings = new AppSettings
        {
            ProcessMonitor = new ProcessMonitorSettings { ProcessName = "starcraft.exe" },
            KeyRepeat = new KeyRepeatSettings
            {
                Default = new KeyRepeatState { RepeatSpeed = 31, RepeatDelay = 1000 },
                FastMode = new KeyRepeatState { RepeatSpeed = 20, RepeatDelay = 500 }
            }
        };

        var optionsMonitor = new TestOptionsMonitor<AppSettings>(appSettings);
        var logger = new Mock<ILogger<ProcessEventWatcher>>();
        var queryBuilder = new SecurityAuditQueryBuilder();
        var watcherFactory = new FakeWrappedWatcher();

        var watcher = new ProcessEventWatcher(
            logger.Object,
            optionsMonitor,
            watcherFactory,
            queryBuilder
        );

        watcher.Configure("starcraft.exe");

        var raisedCount = 0;
        watcher.ProcessEventOccurred += (_, _) => raisedCount++;

        var events = new List<IWrappedEventRecordWrittenEventArgs>
        {
            new FakeWrappedEventRecordWrittenEventArgs(new FakeWrappedEventRecord(4688, 1, "starcraft.exe")),
            new FakeWrappedEventRecordWrittenEventArgs(new FakeWrappedEventRecord(4689, 1, "starcraft.exe")),
            new FakeWrappedEventRecordWrittenEventArgs(new FakeWrappedEventRecord(4688, 2, "starcraft.exe")),
            new FakeWrappedEventRecordWrittenEventArgs(new FakeWrappedEventRecord(4689, 2, "starcraft.exe")),
            new FakeWrappedEventRecordWrittenEventArgs(new FakeWrappedEventRecord(4688, 3, "starcraft.exe"))
        };

        // Act
        foreach (var e in events)
            watcher.HandleEvent(e);

        // Assert
        Assert.Equal(5, raisedCount);
    }

    [Fact]
    public void HandleEvent_ShouldNotRaiseEvent_WhenEventRecordIsNull()
    {
        var logger = new Mock<ILogger<ProcessEventWatcher>>();
        var optionsMonitor = new TestOptionsMonitor<AppSettings>(new AppSettings
        {
            ProcessMonitor = new ProcessMonitorSettings { ProcessName = "starcraft.exe" },
            KeyRepeat = new KeyRepeatSettings
            {
                Default = new KeyRepeatState { RepeatSpeed = 31, RepeatDelay = 1000 },
                FastMode = new KeyRepeatState { RepeatSpeed = 20, RepeatDelay = 500 }
            }
        });
        var watcher = new ProcessEventWatcher(logger.Object, optionsMonitor, new FakeWrappedWatcher(),
            new SecurityAuditQueryBuilder());

        watcher.Configure("starcraft.exe");

        var args = new FakeWrappedEventRecordWrittenEventArgs(null);

        var wasRaised = false;
        watcher.ProcessEventOccurred += (_, _) => wasRaised = true;

        watcher.HandleEvent(args);

        Assert.False(wasRaised, "Event should not be raised when EventRecord is null.");
    }

    [Fact]
    public void HandleEvent_ShouldLogWarning_WhenPropertiesIndexOutOfRange()
    {
        var logger = new Mock<ILogger<ProcessEventWatcher>>();
        var optionsMonitor = new TestOptionsMonitor<AppSettings>(new AppSettings
        {
            ProcessMonitor = new ProcessMonitorSettings { ProcessName = "starcraft.exe" },
            KeyRepeat = new KeyRepeatSettings
            {
                Default = new KeyRepeatState { RepeatSpeed = 31, RepeatDelay = 1000 },
                FastMode = new KeyRepeatState { RepeatSpeed = 20, RepeatDelay = 500 }
            }
        });

        var watcher = new ProcessEventWatcher(logger.Object, optionsMonitor, new FakeWrappedWatcher(),
            new SecurityAuditQueryBuilder());
        watcher.Configure("starcraft.exe");

        var record = new FakeWrappedEventRecord(4688, new List<object?> { null }); // Properties[1] missing
        var args = new FakeWrappedEventRecordWrittenEventArgs(record);

        watcher.HandleEvent(args);

        logger.Verify(log => log.Log(
            LogLevel.Warning,
            It.IsAny<EventId>(),
            MoqLogExtensions.MatchLogState("Missing required properties from wrapped event record."),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }
}