using System.Text;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Sinks.InMemory;
using Xunit;

namespace KeyRepeatTuner.Tests.Logging;

public class LoggingTests
{
    private readonly Logger _logger;
    private readonly InMemorySink _sink;

    public LoggingTests()
    {
        _sink = new InMemorySink();
        _logger = new LoggerConfiguration()
            .WriteTo.Sink(_sink)
            .CreateLogger();
    }

    [Fact]
    public void Logger_ShouldWriteMessages()
    {
        // Arrange
        const string message = "Test log message";

        // Act
        _logger.Information(message);

        // Assert
        var logEvents = _sink.LogEvents.ToList();
        Assert.Single(logEvents);
        Assert.Contains(logEvents, e => e.MessageTemplate.Text.Contains(message));
    }

    [Fact]
    public void Logger_ShouldFormatStructuredLogsCorrectly()
    {
        // Arrange
        const string testUser = "TestUser";

        // Act
        _logger.Information("User {User} performed an action", testUser);

        // Assert
        var logEvent = _sink.LogEvents.FirstOrDefault();
        Assert.NotNull(logEvent);
        Assert.Contains("User \"TestUser\" performed an action", logEvent.RenderMessage());
        Assert.True(logEvent.Timestamp != default, "Expected log entry to contain a timestamp.");
    }

    [Fact]
    public async Task Logger_ShouldHandleConcurrentWrites()
    {
        // Arrange
        var messages = new[] { "Message 1", "Message 2", "Message 3", "Message 4", "Message 5" };

        // Act
        await Task.WhenAll(messages.Select(msg => Task.Run(() => _logger.Information(msg))));

        // Assert
        var logEvents = _sink.LogEvents.ToList();
        Assert.Equal(messages.Length, logEvents.Count);
        foreach (var msg in messages) Assert.Contains(logEvents, e => e.MessageTemplate.Text.Contains(msg));
    }

    [Fact]
    public void Logger_ShouldWriteWarningLogs()
    {
        // Act
        _logger.Warning("This is a warning message");

        // Assert
        Assert.Contains(_sink.LogEvents, e => e.Level == LogEventLevel.Warning);
    }

    [Fact]
    public void Logger_ShouldWriteErrorsWithExceptions()
    {
        // Arrange
        var exception = new InvalidOperationException("Test exception");

        // Act
        _logger.Error(exception, "An error occurred");

        // Assert
        var logEvent = _sink.LogEvents.FirstOrDefault(e => e.Level == LogEventLevel.Error);
        Assert.NotNull(logEvent);
        Assert.Contains("An error occurred", logEvent.RenderMessage());
        Assert.Contains("Test exception", logEvent.Exception?.Message);
    }

    [Fact]
    public void LogFile_ShouldBeUtf8Encoded()
    {
        // Arrange
        var logFilePath = Path.Combine(Path.GetTempPath(), "test_log.txt");
        const string logMessage = "UTF-8 Encoding Test";

        var logger = new LoggerConfiguration()
            .WriteTo.File(logFilePath, encoding: Encoding.UTF8)
            .CreateLogger();

        // Act
        logger.Information(logMessage);
        logger.Dispose();

        // Assert
        var fileBytes = File.ReadAllBytes(logFilePath);
        var fileContent = Encoding.UTF8.GetString(fileBytes);

        Assert.Contains(logMessage, fileContent);
    }
}