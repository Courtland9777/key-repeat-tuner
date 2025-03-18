using System.Text;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Sinks.InMemory;

namespace StarCraftKeyManager.Tests;

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
        var collection = _sink.LogEvents as LogEvent[] ?? [.. _sink.LogEvents];

        Assert.Single(collection);
        Assert.Contains(collection, e => e.MessageTemplate.Text.Contains(message));
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

    [Fact]
    public async Task Logger_ShouldHandleConcurrentWrites()
    {
        // Arrange
        var logFilePath = Path.Combine(Path.GetTempPath(), "concurrent_log.txt");
        var logger = new LoggerConfiguration()
            .WriteTo.File(logFilePath, rollingInterval: RollingInterval.Day)
            .CreateLogger();

        var messages = new[] { "Message 1", "Message 2", "Message 3", "Message 4", "Message 5" };

        // Act
        await Task.Run(() => { Parallel.ForEach(messages, message => { logger.Information(message); }); });

        await logger.DisposeAsync();

        // Assert
        var logContent = await File.ReadAllTextAsync(logFilePath);
        foreach (var message in messages) Assert.Contains(message, logContent);
    }

    [Fact]
    public void Logger_ShouldFormatMessagesCorrectly()
    {
        // Arrange
        const string testUser = "TestUser";

        // Act
        _logger.Information("User {User} logged in", testUser);

        // Assert
        var logEvent = _sink.LogEvents.FirstOrDefault(); // ✅ Use FirstOrDefault()
        Assert.NotNull(logEvent);
        Assert.Contains($"User {testUser} logged in", logEvent.RenderMessage());
    }

    [Fact]
    public void Logger_ShouldWriteWarningLogs()
    {
        // Act
        _logger.Warning("This is a warning message");

        // Assert
        Assert.Contains(_sink.LogEvents, e => e.Level == LogEventLevel.Warning);
    }
}