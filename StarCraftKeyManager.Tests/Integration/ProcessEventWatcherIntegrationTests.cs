using System.Diagnostics.Eventing.Reader;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using StarCraftKeyManager.Interfaces;
using StarCraftKeyManager.Models;
using StarCraftKeyManager.Services;
using StarCraftKeyManager.Tests.TestHelpers;
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
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
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
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
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
    }


    [Fact]
    public void ProcessEventOccurred_ShouldBeRaised_WhenStopEventReceived()
    {
    }

    [Fact]
    public void ProcessEventOccurred_ShouldNotBeRaised_WhenUnknownEventId()
    {
    }

    [Fact]
    public void ProcessEventOccurred_ShouldNotBeRaised_WhenProcessIdIsMissing()
    {
    }

    [Fact]
    public void ProcessEventOccurred_ShouldBeRaised_MultipleTimes_WhenMultipleEventsReceived()
    {
    }

    // Logging behavior tests
    [Fact]
    public void Start_ShouldLogStartMessage()
    {
    }

    [Fact]
    public void Stop_ShouldLogStopMessage()
    {
    }

    [Fact]
    public void EventWatcherOnEventRecordWritten_ShouldLogMissingPropertiesWarning()
    {
    }

    [Fact]
    public void EventWatcherOnEventRecordWritten_ShouldLogNullEventRecordWarning()
    {
    }

    [Fact]
    public void EventWatcherOnEventRecordWritten_ShouldLogProcessEventInfo_WhenValidEvent()
    {
    }

    [Fact]
    public void EventWatcherOnEventRecordWritten_ShouldLogError_WhenExceptionThrown()
    {
    }

    [Fact]
    public void ExtractProcessId_ShouldLogError_WhenAccessingPropertiesFails()
    {
    }

    // Edge and robustness
    [Fact]
    public void EventWatcherOnEventRecordWritten_ShouldNotThrow_WhenEventRecordIsNull()
    {
    }

    [Fact]
    public void Configure_ShouldAllowMultipleCalls_WithoutFailure()
    {
    }

    [Fact]
    public void Start_ShouldNotThrow_WhenEventWatcherIsNull()
    {
    }
}