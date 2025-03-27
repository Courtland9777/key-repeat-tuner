using System.Diagnostics.Eventing.Reader;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using StarCraftKeyManager.Configuration;
using StarCraftKeyManager.Interfaces;
using StarCraftKeyManager.Models;
using StarCraftKeyManager.Services;
using StarCraftKeyManager.SystemAdapters.Wrappers;
using StarCraftKeyManager.Tests.TestUtilities.Extensions;
using StarCraftKeyManager.Tests.TestUtilities.Fakes;
using StarCraftKeyManager.Tests.TestUtilities.Stubs;
using Xunit;

namespace StarCraftKeyManager.Tests.Services.ProcessEventWatcherTests;

public class WatcherLifecycleTests
{
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
    public void Configure_ShouldReplaceWatcher_WhenCalledMultipleTimes()
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

        var factory = new WatcherCaptureFactory();
        var watcher = new ProcessEventWatcher(logger.Object, optionsMonitor, factory, new SecurityAuditQueryBuilder());

        watcher.Configure("starcraft.exe");
        var first = factory.LastCreated;

        watcher.Configure("starcraft.exe");
        var second = factory.LastCreated;

        Assert.NotSame(first, second);
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

    [Fact]
    public void Start_ShouldNotAttachHandler_MultipleTimes_WhenCalledMoreThanOnce()
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

        var countingWatcher = new CountingWrappedWatcher();
        var watcher = new ProcessEventWatcher(logger.Object, optionsMonitor, new TestWatcherFactory(countingWatcher),
            new SecurityAuditQueryBuilder());

        watcher.Configure("starcraft.exe");
        watcher.Start();
        watcher.Start(); // second start should do nothing

        Assert.Equal(1, countingWatcher.AttachCount);
    }

    [Fact]
    public void Stop_ShouldUnsubscribeEventHandler_WhenCalled()
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

        var trackingWatcher = new TrackingWrappedWatcher();
        var watcher = new ProcessEventWatcher(logger.Object, optionsMonitor, new TestWatcherFactory(trackingWatcher),
            new SecurityAuditQueryBuilder());

        watcher.Configure("starcraft.exe");
        watcher.Start();
        watcher.Stop();

        Assert.True(trackingWatcher.Unsubscribed, "Event handler should be unsubscribed on Stop().");
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
    public void Dispose_ShouldBeIdempotent_WhenCalledMultipleTimes()
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
        watcher.Start();

        var ex1 = Record.Exception(() => watcher.Dispose());
        var ex2 = Record.Exception(() => watcher.Dispose());

        Assert.Null(ex1);
        Assert.Null(ex2);
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
}