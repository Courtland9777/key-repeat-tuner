using KeyRepeatTuner.Configuration;
using KeyRepeatTuner.Configuration.ValueObjects;
using KeyRepeatTuner.Monitoring.Interfaces;
using KeyRepeatTuner.Monitoring.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace KeyRepeatTuner.Tests.Core.Services;

public class WatcherHostedServiceTests
{
    private readonly Mock<IHostApplicationLifetime> _mockLifetime = new();
    private readonly Mock<ILogger<WatcherHostedService>> _mockLogger = new();
    private readonly Mock<IOptionsMonitor<AppSettings>> _mockOptionsMonitor = new();
    private readonly Mock<IProcessEventRouter> _mockRouter = new();
    private readonly Mock<IProcessEventWatcher> _mockWatcher = new();

    public WatcherHostedServiceTests()
    {
        var settings = new AppSettings
        {
            ProcessNames = [new ProcessName("notepad")],
            KeyRepeat = new KeyRepeatSettings
            {
                Default = new KeyRepeatState { RepeatSpeed = 20, RepeatDelay = 750 },
                FastMode = new KeyRepeatState { RepeatSpeed = 30, RepeatDelay = 500 }
            }
        };

        _mockOptionsMonitor.Setup(m => m.CurrentValue).Returns(settings);
    }

    private WatcherHostedService CreateSut()
    {
        var trigger = new StartupWatcherTrigger(
            _mockOptionsMonitor.Object,
            _mockWatcher.Object,
            _mockRouter.Object
        );

        return new WatcherHostedService(
            _mockLogger.Object,
            trigger,
            _mockWatcher.Object,
            _mockLifetime.Object);
    }

    [Fact]
    public async Task StartAsync_ShouldTriggerStartupLogic()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var result = await Record.ExceptionAsync(() => sut.StartAsync(CancellationToken.None));

        // Assert
        Assert.Null(result);
        _mockWatcher.Verify(w => w.OnSettingsChanged(It.IsAny<AppSettings>()), Times.Once);
        _mockRouter.Verify(r => r.OnStartup(), Times.Once);
    }

    [Fact]
    public async Task StopAsync_ShouldDisposeWatcher()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var result = await Record.ExceptionAsync(() => sut.StopAsync(CancellationToken.None));

        // Assert
        Assert.Null(result);
        _mockWatcher.Verify(w => w.Dispose(), Times.Once);
    }
}