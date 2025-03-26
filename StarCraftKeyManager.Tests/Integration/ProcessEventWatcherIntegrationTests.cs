using System.Diagnostics.Eventing.Reader;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using StarCraftKeyManager.Adapters;
using StarCraftKeyManager.Configuration;
using StarCraftKeyManager.Events;
using StarCraftKeyManager.Interfaces;
using StarCraftKeyManager.Models;
using StarCraftKeyManager.Services;
using StarCraftKeyManager.SystemAdapters.Wrappers;
using StarCraftKeyManager.Tests.TestUtilities.Extensions;
using StarCraftKeyManager.Tests.TestUtilities.Fakes;
using StarCraftKeyManager.Tests.TestUtilities.Stubs;
using Xunit;

namespace StarCraftKeyManager.Tests.Integration;

public class ProcessEventWatcherIntegrationTests
{
    private readonly Mock<ILogger<ProcessEventWatcher>> _mockLogger = new();
    private readonly Mock<IOptionsMonitor<AppSettings>> _mockOptionsMonitor = new();
    private readonly Mock<IEventLogQueryBuilder> _mockQueryBuilder = new();
    private readonly Mock<IEventWatcherFactory> _mockWatcherFactory = new();
    private readonly ProcessEventWatcher _processEventWatcher;


    public ProcessEventWatcherIntegrationTests()
    {
        _mockOptionsMonitor.Setup(m => m.CurrentValue).Returns(new AppSettings
        {
            KeyRepeat = new KeyRepeatSettings
            {
                Default = new KeyRepeatState { RepeatDelay = 1000, RepeatSpeed = 31 },
                FastMode = new KeyRepeatState { RepeatDelay = 500, RepeatSpeed = 20 }
            },
            ProcessMonitor = new ProcessMonitorSettings { ProcessName = "starcraft.exe" }
        });

        _mockQueryBuilder.Setup(b => b.BuildQuery())
            .Returns(new EventLogQuery("Security", PathType.LogName, "MockQuery"));

        _processEventWatcher = new ProcessEventWatcher(
            _mockLogger.Object,
            _mockOptionsMonitor.Object,
            _mockWatcherFactory.Object,
            _mockQueryBuilder.Object
        );
    }

    // Lifecycle tests
    [Fact]
    public void Configure_ShouldCreateWatcher_UsingQueryBuilderResult()
    {
        // Arrange
        var expectedQuery = new EventLogQuery("Security", PathType.LogName);

        var mockQueryBuilder = new Mock<IEventLogQueryBuilder>();
        mockQueryBuilder.Setup(b => b.BuildQuery()).Returns(expectedQuery);

        EventLogQuery? capturedQuery = null;

        var mockWatcherFactory = new Mock<IEventWatcherFactory>();
        mockWatcherFactory.Setup(f => f.Create(It.IsAny<EventLogQuery>()))
            .Callback<EventLogQuery>(q => capturedQuery = q)
            .Returns(new WrappedEventLogWatcher(new EventLogWatcher(expectedQuery)));


        var watcher = new ProcessEventWatcher(
            Mock.Of<ILogger<ProcessEventWatcher>>(),
            Mock.Of<IOptionsMonitor<AppSettings>>(),
            mockWatcherFactory.Object,
            mockQueryBuilder.Object);

        // Act
        watcher.Configure("starcraft.exe");

        // Assert
        Assert.NotNull(capturedQuery);
        Assert.Same(expectedQuery, capturedQuery);
    }


    [Fact]
    public void Start_ShouldEnableEventWatcher_WhenNotAlreadyStarted()
    {
        // Arrange
        var query = new EventLogQuery("Security", PathType.LogName);

        var mockQueryBuilder = new Mock<IEventLogQueryBuilder>();
        mockQueryBuilder.Setup(q => q.BuildQuery()).Returns(query);

        var mockLogger = new Mock<ILogger<ProcessEventWatcher>>();

        var mockFactory = new Mock<IEventWatcherFactory>();
        var watcher = new EventLogWatcher(query);
        var wrappedWatcher = new WrappedEventLogWatcher(watcher);
        mockFactory.Setup(f => f.Create(query)).Returns(wrappedWatcher);


        var sut = new ProcessEventWatcher(
            mockLogger.Object,
            Mock.Of<IOptionsMonitor<AppSettings>>(),
            mockFactory.Object,
            mockQueryBuilder.Object);

        sut.Configure("starcraft.exe");

        // Act
        sut.Start();

        // Assert
        mockLogger.Verify(log => log.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                MoqLogExtensions.MatchLogState("Process event watcher started."),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }


    [Fact]
    public void Start_ShouldNotEnableEventWatcher_WhenAlreadyStarted()
    {
        // Arrange
        var query = new EventLogQuery("Security", PathType.LogName);

        var mockQueryBuilder = new Mock<IEventLogQueryBuilder>();
        mockQueryBuilder.Setup(q => q.BuildQuery()).Returns(query);

        var mockLogger = new Mock<ILogger<ProcessEventWatcher>>();

        var mockFactory = new Mock<IEventWatcherFactory>();
        var watcher = new EventLogWatcher(query);
        var wrappedWatcher = new WrappedEventLogWatcher(watcher);
        mockFactory.Setup(f => f.Create(query)).Returns(wrappedWatcher);

        var sut = new ProcessEventWatcher(
            mockLogger.Object,
            Mock.Of<IOptionsMonitor<AppSettings>>(),
            mockFactory.Object,
            mockQueryBuilder.Object);

        sut.Configure("starcraft.exe");

        // Act
        sut.Start();
        sut.Start();

        // Assert
        mockLogger.Verify(log => log.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            MoqLogExtensions.MatchLogState("Process event watcher started."),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }


    [Fact]
    public void Stop_ShouldDisableEventWatcher_WhenStarted()
    {
        // Arrange
        var mockWatcher = new Mock<IWrappedEventLogWatcher>();
        var mockFactory = new Mock<IEventWatcherFactory>();
        var mockQueryBuilder = new Mock<IEventLogQueryBuilder>();
        var mockLogger = new Mock<ILogger<ProcessEventWatcher>>();

        var query = new EventLogQuery("Security", PathType.LogName);
        mockQueryBuilder.Setup(q => q.BuildQuery()).Returns(query);
        mockFactory.Setup(f => f.Create(query)).Returns(mockWatcher.Object);

        var sut = new ProcessEventWatcher(
            mockLogger.Object,
            Mock.Of<IOptionsMonitor<AppSettings>>(),
            mockFactory.Object,
            mockQueryBuilder.Object);

        sut.Configure("starcraft.exe");
        sut.Start();

        // Act
        sut.Stop();

        // Assert
        mockWatcher.VerifySet(w => w.Enabled = false, Times.Once);
        mockWatcher.Verify(w => w.Dispose(), Times.Once);

        mockLogger.Verify(log => log.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            MoqLogExtensions.MatchLogState("Process event watcher stopped."),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }


    [Fact]
    public void Stop_ShouldNotThrow_WhenNotStarted()
    {
        // Arrange
        var query = new EventLogQuery("Security", PathType.LogName);

        var mockQueryBuilder = new Mock<IEventLogQueryBuilder>();
        mockQueryBuilder.Setup(q => q.BuildQuery()).Returns(query);

        var mockLogger = new Mock<ILogger<ProcessEventWatcher>>();

        var mockFactory = new Mock<IEventWatcherFactory>();
        var mockWatcher = new Mock<IWrappedEventLogWatcher>();
        mockFactory.Setup(f => f.Create(query)).Returns(mockWatcher.Object);

        var sut = new ProcessEventWatcher(
            mockLogger.Object,
            Mock.Of<IOptionsMonitor<AppSettings>>(),
            mockFactory.Object,
            mockQueryBuilder.Object);

        // Configure but do not call Start()
        sut.Configure("starcraft.exe");

        // Act + Assert
        var ex = Record.Exception(() => sut.Stop());
        Assert.Null(ex); // should not throw
    }


    [Fact]
    public void Dispose_ShouldCallStop()
    {
        // Arrange
        var query = new EventLogQuery("Security", PathType.LogName);

        var mockQueryBuilder = new Mock<IEventLogQueryBuilder>();
        mockQueryBuilder.Setup(q => q.BuildQuery()).Returns(query);

        var mockLogger = new Mock<ILogger<ProcessEventWatcher>>();

        var mockWatcher = new Mock<IWrappedEventLogWatcher>();
        var mockFactory = new Mock<IEventWatcherFactory>();
        mockFactory.Setup(f => f.Create(query)).Returns(mockWatcher.Object);

        var sut = new ProcessEventWatcher(
            mockLogger.Object,
            Mock.Of<IOptionsMonitor<AppSettings>>(),
            mockFactory.Object,
            mockQueryBuilder.Object);

        sut.Configure("starcraft.exe");
        sut.Start();

        // Act
        sut.Dispose();

        // Assert
        mockWatcher.VerifySet(w => w.Enabled = false, Times.Once);
        mockWatcher.Verify(w => w.Dispose(), Times.Once);

        mockLogger.Verify(log => log.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                MoqLogExtensions.MatchLogState("Process event watcher stopped."),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }


    // Event raising tests
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


    // Logging behavior tests
    [Fact]
    public void Start_ShouldLogStartMessage()
    {
        // Arrange
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

        var queryBuilder = new SecurityAuditQueryBuilder();
        var watcherFactory = new FakeWrappedWatcher();

        var watcher = new ProcessEventWatcher(logger.Object, optionsMonitor, watcherFactory, queryBuilder);
        watcher.Configure("starcraft.exe");

        // Act
        watcher.Start();

        // Assert
        logger.Verify(log => log.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            MoqLogExtensions.MatchLogState("Process event watcher started."),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }


    [Fact]
    public void Stop_ShouldLogStopMessage()
    {
        // Arrange
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

        var queryBuilder = new SecurityAuditQueryBuilder();
        var watcherFactory = new FakeWrappedWatcher();

        var watcher = new ProcessEventWatcher(logger.Object, optionsMonitor, watcherFactory, queryBuilder);
        watcher.Configure("starcraft.exe");
        watcher.Start(); // must start before stopping

        // Act
        watcher.Stop();

        // Assert
        logger.Verify(log => log.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            MoqLogExtensions.MatchLogState("Process event watcher stopped."),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }


    [Fact]
    public void EventWatcherOnEventRecordWritten_ShouldLogMissingPropertiesWarning()
    {
        // Arrange
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

        var watcher = new ProcessEventWatcher(
            logger.Object,
            optionsMonitor,
            new FakeWrappedWatcher(),
            new SecurityAuditQueryBuilder()
        );

        watcher.Configure("starcraft.exe");

        // Simulate event with missing index 1
        var badRecord = new FakeWrappedEventRecord(4688, [null /* 0 only */]);
        var args = new FakeWrappedEventRecordWrittenEventArgs(badRecord);

        // Act
        watcher.HandleEvent(args);

        // Assert
        logger.Verify(log => log.Log(
            LogLevel.Warning,
            It.IsAny<EventId>(),
            MoqLogExtensions.MatchLogState("Missing required properties from wrapped event record."),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }


    [Fact]
    public void EventWatcherOnEventRecordWritten_ShouldLogNullEventRecordWarning()
    {
        // Arrange
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
        var watcher = new ProcessEventWatcher(
            logger.Object,
            optionsMonitor,
            new FakeWrappedWatcher(),
            new SecurityAuditQueryBuilder()
        );

        watcher.Configure("starcraft.exe");

        var args = new FakeWrappedEventRecordWrittenEventArgs(null); // simulate null EventRecord

        // Act
        watcher.HandleEvent(args);

        // Assert
        logger.Verify(log => log.Log(
            LogLevel.Warning,
            It.IsAny<EventId>(),
            MoqLogExtensions.MatchLogState("Null EventRecord received from event log."),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }


    [Fact]
    public void EventWatcherOnEventRecordWritten_ShouldLogProcessEventInfo_WhenValidEvent()
    {
        // Arrange
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

        var watcher = new ProcessEventWatcher(
            logger.Object,
            optionsMonitor,
            new FakeWrappedWatcher(),
            new SecurityAuditQueryBuilder()
        );

        watcher.Configure("starcraft.exe");

        var valid = new FakeWrappedEventRecord(4688, 1234, "starcraft.exe");
        var args = new FakeWrappedEventRecordWrittenEventArgs(valid);

        // Act
        watcher.HandleEvent(args);

        // Assert
        logger.Verify(log => log.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            MoqLogExtensions.MatchLogState("Detected process event: EventId=4688, ProcessId=1234"),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }


    [Fact]
    public void EventWatcherOnEventRecordWritten_ShouldLogError_WhenExceptionThrown()
    {
        // Arrange
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

        var watcher = new ProcessEventWatcher(
            logger.Object,
            optionsMonitor,
            new FakeWrappedWatcher(),
            new SecurityAuditQueryBuilder()
        );

        watcher.Configure("starcraft.exe");

        var broken = new FakeWrappedEventRecord(4688, "not-a-number", "starcraft.exe");
        var args = new FakeWrappedEventRecordWrittenEventArgs(broken);

        // Act
        watcher.HandleEvent(args);

        // Assert
        logger.Verify(log => log.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            MoqLogExtensions.MatchLogState("Error extracting process ID from wrapped event record."),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }


    [Fact]
    public void ExtractProcessId_ShouldLogError_WhenAccessingPropertiesFails()
    {
        // Arrange
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
        var watcher = new ProcessEventWatcher(
            logger.Object,
            optionsMonitor,
            new FakeWrappedWatcher(),
            new SecurityAuditQueryBuilder()
        );

        var brokenRecord = new ThrowingWrappedEventRecord();

        // Use reflection to call private ExtractProcessId(IWrappedEventRecord)
        var method = typeof(ProcessEventWatcher)
            .GetMethod("ExtractProcessId", BindingFlags.NonPublic | BindingFlags.Instance, null,
                [typeof(IWrappedEventRecord)], null)!;

        // Act
        var result = method.Invoke(watcher, [brokenRecord]);

        // Assert
        Assert.Null(result); // extraction failed, returned null

        logger.Verify(log => log.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            MoqLogExtensions.MatchLogState("Error extracting process ID from wrapped event record."),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }


    // Edge and robustness
    [Fact]
    public void EventWatcherOnEventRecordWritten_ShouldNotThrow_WhenEventRecordIsNull()
    {
        // Arrange
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
        var watcher = new ProcessEventWatcher(
            logger.Object,
            optionsMonitor,
            new FakeWrappedWatcher(),
            new SecurityAuditQueryBuilder()
        );

        watcher.Configure("starcraft.exe");

        var args = new FakeWrappedEventRecordWrittenEventArgs(null);

        // Act & Assert
        var ex = Record.Exception(() => watcher.HandleEvent(args));
        Assert.Null(ex); // should not throw
    }


    [Fact]
    public void Configure_ShouldAllowMultipleCalls_WithoutFailure()
    {
        // Arrange
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
        var watcher = new ProcessEventWatcher(
            logger.Object,
            optionsMonitor,
            new FakeWrappedWatcher(),
            new SecurityAuditQueryBuilder()
        );

        // Act
        var ex1 = Record.Exception(() => watcher.Configure("starcraft.exe"));
        var ex2 = Record.Exception(() => watcher.Configure("starcraft.exe"));

        // Assert
        Assert.Null(ex1);
        Assert.Null(ex2);
    }


    [Fact]
    public void Start_ShouldNotThrow_WhenEventWatcherIsNull()
    {
        // Arrange
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
        var watcher = new ProcessEventWatcher(
            logger.Object,
            optionsMonitor,
            new FakeWrappedWatcher(),
            new SecurityAuditQueryBuilder()
        );

        // Act & Assert
        var ex = Record.Exception(() => watcher.Start());
        Assert.Null(ex); // should not throw even if not configured
    }
}