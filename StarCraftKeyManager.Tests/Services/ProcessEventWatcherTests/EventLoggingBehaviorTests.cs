using System.Reflection;
using Microsoft.Extensions.Logging;
using Moq;
using StarCraftKeyManager.Adapters;
using StarCraftKeyManager.Configuration;
using StarCraftKeyManager.Models;
using StarCraftKeyManager.Services;
using StarCraftKeyManager.Tests.TestUtilities.Extensions;
using StarCraftKeyManager.Tests.TestUtilities.Fakes;
using StarCraftKeyManager.Tests.TestUtilities.Stubs;
using Xunit;

namespace StarCraftKeyManager.Tests.Services.ProcessEventWatcherTests;

public class EventLoggingBehaviorTests
{
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
}