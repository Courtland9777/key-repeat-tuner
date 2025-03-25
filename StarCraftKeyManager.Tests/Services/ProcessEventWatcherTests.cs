using System.Diagnostics.Eventing.Reader;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using StarCraftKeyManager.Adapters;
using StarCraftKeyManager.Configuration;
using StarCraftKeyManager.Interfaces;
using StarCraftKeyManager.Models;
using StarCraftKeyManager.Services;
using StarCraftKeyManager.Tests.TestHelpers;
using StarCraftKeyManager.Tests.TestUtilities.Fakes;
using Xunit;

namespace StarCraftKeyManager.Tests.Services;

public class ProcessEventWatcherTests
{
    private readonly Mock<ILogger<ProcessEventWatcher>> _mockLogger = new();
    private readonly Mock<IOptionsMonitor<AppSettings>> _mockOptionsMonitor = new();
    private readonly Mock<IEventLogQueryBuilder> _mockQueryBuilder = new();
    private readonly Mock<IEventWatcherFactory> _mockWatcherFactory = new();
    private readonly ProcessEventWatcher _watcher;

    public ProcessEventWatcherTests()
    {
        _mockOptionsMonitor.Setup(o => o.CurrentValue).Returns(AppSettingsFactory.CreateDefault());

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

        _watcher.HandleEvent(startArgs);
        _watcher.HandleEvent(stopArgs);

        Assert.Equal(2, raised);
    }


    [Fact]
    public void Event_ShouldBeRaised_ButNotTriggerKeySettings_ForUnknownEventId()
    {
        // Arrange
        var raised = false;
        _watcher.ProcessEventOccurred += (_, _) => raised = true;

        var unknownEvent = new FakeWrappedEventRecordWrittenEventArgs(9999, "starcraft.exe");

        // Act
        _watcher.HandleEvent(unknownEvent);

        // Assert
        Assert.True(raised);
    }

    [Fact]
    public async Task OnProcessEventOccurred_ShouldNotApplySettings_ForUnknownEventId()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<ProcessMonitorService>>();
        var mockEventWatcher = new Mock<IProcessEventWatcher>();
        var mockKeyboardSettings = new Mock<IKeyboardSettingsApplier>();
        var mockProcessProvider = new Mock<IProcessProvider>();

        var settings = new AppSettings
        {
            ProcessMonitor = new ProcessMonitorSettings { ProcessName = "starcraft.exe" },
            KeyRepeat = new KeyRepeatSettings
            {
                Default = new KeyRepeatState { RepeatSpeed = 31, RepeatDelay = 1000 },
                FastMode = new KeyRepeatState { RepeatSpeed = 20, RepeatDelay = 500 }
            }
        };

        var optionsMonitor = new TestOptionsMonitor<AppSettings>(settings);

        mockProcessProvider
            .Setup(p => p.GetProcessIdsByName("starcraft"))
            .Returns([]);

        var service = new ProcessMonitorService(
            mockLogger.Object,
            optionsMonitor,
            mockEventWatcher.Object,
            mockKeyboardSettings.Object,
            mockProcessProvider.Object
        );

        await service.StartAsync(CancellationToken.None);

        mockKeyboardSettings.Invocations.Clear();

        // Act
        mockEventWatcher.Raise(
            w => w.ProcessEventOccurred += null,
            new ProcessEventArgs(9999, 1234, "starcraft.exe")
        );

        // Assert
        mockKeyboardSettings.Verify(k =>
            k.ApplyRepeatSettings(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }


    [Fact]
    public void ShouldLogWarning_WhenEventRecordIsNull()
    {
        // Arrange
        var fakeArgs = new FakeWrappedEventRecordWrittenEventArgs(null);

        // Act
        _watcher.HandleEvent(fakeArgs);

        // Assert
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
        // Arrange
        var record = new FakeWrappedEventRecord(4688, "not-a-number", "starcraft.exe");
        var args = new FakeWrappedEventRecordWrittenEventArgs(record);

        // Act
        _watcher.HandleEvent(args);

        // Assert: verify log message using MoqLogExtensions
        _mockLogger.Verify(l => l.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            MoqLogExtensions.MatchLogState("Error extracting process ID from wrapped event record"),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }


    [Fact]
    public async Task MultipleEvents_ShouldAllTriggerEvent()
    {
        // Arrange
        var count = 0;
        _watcher.ProcessEventOccurred += (_, _) => count++;

        var events = new List<IWrappedEventRecordWrittenEventArgs>
        {
            new FakeWrappedEventRecordWrittenEventArgs(new FakeWrappedEventRecord(4688, 1, "starcraft.exe")),
            new FakeWrappedEventRecordWrittenEventArgs(new FakeWrappedEventRecord(4688, 2, "starcraft.exe")),
            new FakeWrappedEventRecordWrittenEventArgs(new FakeWrappedEventRecord(4689, 1, "starcraft.exe")),
            new FakeWrappedEventRecordWrittenEventArgs(new FakeWrappedEventRecord(4689, 2, "starcraft.exe")),
            new FakeWrappedEventRecordWrittenEventArgs(new FakeWrappedEventRecord(4688, 3, "starcraft.exe"))
        };

        // Act
        await Task.Run(() =>
        {
            foreach (var e in events)
                _watcher.HandleEvent(e);
        });

        // Assert
        Assert.Equal(5, count);
    }
}