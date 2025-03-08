using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StarCraftKeyManager.Models;
using Xunit;
using Moq;
using StarCraftKeyManager.Services;

namespace StarCraftKeyManager.Tests;

public class ProcessMonitorTests
{
    private readonly Mock<ILogger<ProcessMonitorService>> _mockLogger = new();
    private readonly Mock<IOptionsMonitor<AppSettings>> _mockOptionsMonitor = new();

    public ProcessMonitorTests()
    {
        var appSettings = new AppSettings
        {
            ProcessMonitor = new ProcessMonitorSettings { ProcessName = "notepad.exe" },
            KeyRepeat = new KeyRepeatSettings
            {
                Default = new KeyRepeatState { RepeatSpeed = 31, RepeatDelay = 1000 },
                FastMode = new KeyRepeatState { RepeatSpeed = 20, RepeatDelay = 500 }
            }
        };

        _mockOptionsMonitor.Setup(m => m.CurrentValue).Returns(appSettings);
    }

    [Fact]
    public async Task ProcessMonitor_ShouldDetectProcessStartAndStop()
    {
        var monitorService = new ProcessMonitorService(_mockLogger.Object, _mockOptionsMonitor.Object);
        var cancellationTokenSource = new CancellationTokenSource();

        // Run the monitoring service in the background
        var task = monitorService.StartAsync(cancellationTokenSource.Token);

        // Simulate launching Notepad
        var process = Process.Start("notepad.exe");
        Assert.NotNull(process);
        await Task.Delay(2000, cancellationTokenSource.Token); // Give it time to detect

        // Simulate closing Notepad
        process.Kill();
        await Task.Delay(2000, cancellationTokenSource.Token); // Give it time to detect

        // Stop monitoring
        await cancellationTokenSource.CancelAsync();
        await task;

        // Validate that logs contain process start and stop events
        _mockLogger.Verify(log => log.LogInformation("Applying Key Repeat Settings: RepeatSpeed=20, RepeatDelay=500"), Times.AtLeastOnce());
        _mockLogger.Verify(log => log.LogInformation("Applying Key Repeat Settings: RepeatSpeed=31, RepeatDelay=1000"), Times.AtLeastOnce());
    }
}