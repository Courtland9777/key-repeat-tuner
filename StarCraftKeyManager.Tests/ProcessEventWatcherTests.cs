using System.Diagnostics.Eventing.Reader;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Moq;
using StarCraftKeyManager.Models;
using StarCraftKeyManager.Services;

namespace StarCraftKeyManager.Tests;

public class ProcessEventWatcherTests
{
    private readonly List<ProcessEventArgs> _capturedEvents;
    private readonly Mock<ILogger<ProcessEventWatcher>> _mockLogger;
    private readonly ProcessEventWatcher _processEventWatcher;

    public ProcessEventWatcherTests()
    {
        _mockLogger = new Mock<ILogger<ProcessEventWatcher>>();
        _capturedEvents = [];
        _processEventWatcher = new ProcessEventWatcher(_mockLogger.Object);
        _processEventWatcher.ProcessEventOccurred += (_, args) => _capturedEvents.Add(args);
    }

    [Fact]
    public void Configure_ShouldSetProcessName()
    {
        // Arrange
        var processName = "testProcess.exe";

        // Act
        _processEventWatcher.Configure(processName);

        // Assert
        Assert.Equal("testProcess", GetPrivateField<string>(_processEventWatcher, "_processName"));
    }

    [Fact]
    public void Start_ShouldLogInformation()
    {
        // Act
        _processEventWatcher.Start();

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                It.Is<LogLevel>(logLevel => logLevel == LogLevel.Information),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((@object, type) => @object.ToString()!.Contains("Process event watcher started")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((@object, exception) => true)),
            Times.Once);
    }

    [Fact]
    public void Stop_ShouldLogInformation()
    {
        // Arrange
        _processEventWatcher.Start();

        // Act
        _processEventWatcher.Stop();

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                It.Is<LogLevel>(logLevel => logLevel == LogLevel.Information),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((@object, type) => @object.ToString()!.Contains("Process event watcher stopped")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((@object, exception) => true)),
            Times.Once);
    }

    [Fact]
    public async Task EventWatcherOnEventRecordWrittenAsync_ShouldTriggerEvent()
    {
        // Arrange
        var eventArgs = CreateMockEventRecord(4688, 1234, "testProcess");

        // Act
        await InvokePrivateMethodAsync(_processEventWatcher, "EventWatcherOnEventRecordWrittenAsync", eventArgs);

        // Assert
        Assert.Single(_capturedEvents);
        Assert.Equal(4688, _capturedEvents[0].EventId);
        Assert.Equal(1234, _capturedEvents[0].ProcessId);
        Assert.Equal("testProcess", _capturedEvents[0].ProcessName);
    }

    private static EventRecordWrittenEventArgs CreateMockEventRecord(int eventId, int processId, string processName)
    {
        var mockEventRecord = new Mock<EventRecord>();
        mockEventRecord.Setup(e => e.Id).Returns(eventId);

        var eventProperties = new object?[] { null, null, null, processId, processName }
            .Select(CreateEventProperty).ToList();

        mockEventRecord.Setup(e => e.Properties).Returns(eventProperties);

        var mockEventArgs = new Mock<EventRecordWrittenEventArgs>();
        mockEventArgs.Setup(e => e.EventRecord).Returns(mockEventRecord.Object);
        return mockEventArgs.Object;
    }

    private static EventProperty CreateEventProperty(object? value)
    {
        var constructor = typeof(EventProperty).GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)
            .FirstOrDefault();

        return constructor != null
            ? (EventProperty?)constructor.Invoke([value]) ??
              throw new InvalidOperationException("Failed to create EventProperty instance.")
            : throw new InvalidOperationException("Could not find a valid constructor for EventProperty.");
    }

    private static T GetPrivateField<T>(object obj, string fieldName)
    {
        var field = obj.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        return field != null
            ? (T)field.GetValue(obj)!
            : throw new InvalidOperationException($"Field '{fieldName}' not found in {obj.GetType().Name}.");
    }

    private static async Task InvokePrivateMethodAsync(object obj, string methodName, params object[] parameters)
    {
        var method = obj.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
        if (method != null)
        {
            if (method.Invoke(obj, parameters) is not Task task)
                throw new InvalidOperationException($"Method '{methodName}' did not return a Task.");
            {
                await task;
            }
        }
        else
        {
            throw new InvalidOperationException($"Method '{methodName}' not found in {obj.GetType().Name}.");
        }
    }
}