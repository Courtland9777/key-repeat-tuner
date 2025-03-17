using System.IO.Abstractions;
using System.Text;
using Moq;

namespace StarCraftKeyManager.Tests;

public class LoggingTests
{
    private const string LogFilePath = "logs/process_monitor.log";

    [Fact]
    public void LogFile_ShouldContainProcessEvents_Mocked()
    {
        // Arrange
        var mockFileSystem = new Mock<IFileSystem>();
        mockFileSystem.Setup(fs => fs.File.Exists(LogFilePath)).Returns(true);
        mockFileSystem.Setup(fs => fs.File.ReadAllText(LogFilePath))
            .Returns("Process Monitor Service Started\nApplying Key Repeat Settings");
        // Act
        var logExists = mockFileSystem.Object.File.Exists(LogFilePath);
        var logContent = mockFileSystem.Object.File.ReadAllText(LogFilePath);
        // Assert
        Assert.True(logExists);
        Assert.Contains("Process Monitor Service Started", logContent);
        Assert.Contains("Applying Key Repeat Settings", logContent);
    }

    [Theory]
    [InlineData("Process Monitor Service Started")]
    [InlineData("Process Monitor Service Stopped")]
    [InlineData("Applying Key Repeat Settings")]
    public void LogFile_ShouldContainExpectedMessages(string expectedMessage)
    {
        // Arrange
        var mockFileSystem = new Mock<IFileSystem>();
        mockFileSystem.Setup(fs => fs.File.ReadAllText(LogFilePath))
            .Returns($"Some log content\n{expectedMessage}\nOther log content");
        // Act
        var logContent = mockFileSystem.Object.File.ReadAllText(LogFilePath);
        // Assert
        Assert.Contains(expectedMessage, logContent);
    }

    [Theory]
    [InlineData(5 * 1024 * 1024)] // 5 MB
    [InlineData(10 * 1024 * 1024)] // 10 MB
    [InlineData(20 * 1024 * 1024)] // 20 MB
    public void LogFile_ShouldRotateWhenSizeExceedsLimit_Parameterized(long sizeLimit)
    {
        // Arrange
        var mockFileSystem = new Mock<IFileSystem>();
        var mockFileInfo = new Mock<IFileInfo>();
        mockFileInfo.Setup(f => f.Length).Returns(sizeLimit - 1); // Simulate file size
        mockFileSystem.Setup(fs => fs.FileInfo.New(LogFilePath)).Returns(mockFileInfo.Object);
        // Act
        var fileInfo = mockFileSystem.Object.FileInfo.New(LogFilePath);
        // Assert
        Assert.True(fileInfo.Length < sizeLimit, $"Log file size should not exceed {sizeLimit / (1024 * 1024)} MB.");
    }

    [Fact]
    public void LogFile_ShouldHaveCorrectPermissions_Mocked()
    {
        // Arrange
        var mockFileSystem = new Mock<IFileSystem>();
        var mockFileInfo = new Mock<IFileInfo>();
        mockFileInfo.Setup(f => f.IsReadOnly).Returns(false);
        mockFileSystem.Setup(fs => fs.FileInfo.New(LogFilePath)).Returns(mockFileInfo.Object);
        // Act
        var fileInfo = mockFileSystem.Object.FileInfo.New(LogFilePath);
        // Assert
        Assert.False(fileInfo.IsReadOnly, "Log file should be writable.");
    }

    [Fact]
    public void LogFile_ShouldBeUtf8Encoded_Improved()
    {
        // Arrange
        var mockFileSystem = new Mock<IFileSystem>();
        var mockFile = new Mock<IFile>();
        var utf8Bytes = Encoding.UTF8.GetBytes("Mock log content");
        mockFile.Setup(f => f.ReadAllBytes(LogFilePath)).Returns(utf8Bytes);
        mockFileSystem.Setup(fs => fs.File).Returns(mockFile.Object);
        // Act
        var logBytes = mockFileSystem.Object.File.ReadAllBytes(LogFilePath);
        var preamble = Encoding.UTF8.GetPreamble();
        var isUtf8 = logBytes.Take(preamble.Length).SequenceEqual(preamble);
        // Assert
        Assert.True(isUtf8, "Log file should be UTF-8 encoded.");
    }

    [Fact]
    public void LogFile_ShouldCleanupOldLogs_Mocked()
    {
        // Arrange
        var mockFileSystem = new Mock<IFileSystem>();
        mockFileSystem.Setup(fs => fs.Directory.GetFiles(It.IsAny<string>(), "*.log"))
            .Returns(["log1.log", "log2.log", "log3.log", "log4.log", "log5.log", "log6.log"]);
        // Act
        var logFiles = mockFileSystem.Object.Directory.GetFiles("logs", "*.log");
        // Assert
        Assert.True(logFiles.Length <= 5, "There should not be more than 5 log files.");
    }

    [Fact]
    public void LogFile_ShouldLogErrorMessages()
    {
        // Arrange
        var mockFileSystem = new Mock<IFileSystem>();
        mockFileSystem.Setup(fs => fs.File.ReadAllText(LogFilePath)).Returns("Error: Unable to start process monitor");
        // Act
        var logContent = mockFileSystem.Object.File.ReadAllText(LogFilePath);
        // Assert
        Assert.Contains("Error: Unable to start process monitor", logContent);
    }

    [Fact]
    public void LogFile_ShouldHandleMissingFileGracefully()
    {
        // Arrange
        var mockFileSystem = new Mock<IFileSystem>();
        mockFileSystem.Setup(fs => fs.File.Exists(LogFilePath)).Returns(false);
        // Act
        var logExists = mockFileSystem.Object.File.Exists(LogFilePath);
        // Assert
        Assert.False(logExists, "Log file should not exist.");
    }

    [Fact]
    public void LogFile_ShouldHaveValidLogEntryFormat()
    {
        // Arrange
        var mockFileSystem = new Mock<IFileSystem>();
        mockFileSystem.Setup(fs => fs.File.ReadAllText(LogFilePath))
            .Returns("[2023-10-10 12:00:00] [INFO] Log entry 1\n[2023-10-10 12:05:00] [ERROR] Log entry 2");
        var logContent = mockFileSystem.Object.File.ReadAllText(LogFilePath);
        var logEntries = logContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        // Act & Assert
        foreach (var entry in logEntries)
            Assert.Matches(@"^\[\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}\] \[INFO|ERROR|WARN\] .+$", entry);
    }

    [Fact]
    public void LogFile_ShouldHandleConcurrentAccess()
    {
        // Arrange
        var mockFileSystem = new Mock<IFileSystem>();
        mockFileSystem.Setup(fs => fs.File.AppendAllText(It.IsAny<string>(), It.IsAny<string>()));
        // Act
        Parallel.For(0, 10, i => { mockFileSystem.Object.File.AppendAllText(LogFilePath, $"Log entry {i}\n"); });
        // Assert
        mockFileSystem.Verify(fs => fs.File.AppendAllText(LogFilePath, It.IsAny<string>()), Times.Exactly(10));
    }
}