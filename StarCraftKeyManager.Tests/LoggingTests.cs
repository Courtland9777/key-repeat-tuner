namespace StarCraftKeyManager.Tests;

public class LoggingTests
{
    private const string LogFilePath = "logs/process_monitor.log";

    [Fact]
    public void LogFile_ShouldContainProcessEvents()
    {
        Assert.True(File.Exists(LogFilePath), "Expected log file to exist.");

        var logContent = File.ReadAllText(LogFilePath);
        Assert.Contains("Process Monitor Service Started", logContent);
        Assert.Contains("Applying Key Repeat Settings", logContent);
    }
}