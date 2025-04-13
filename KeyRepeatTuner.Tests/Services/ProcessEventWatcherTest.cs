using KeyRepeatTuner.Configuration;
using KeyRepeatTuner.Interfaces;
using KeyRepeatTuner.Services;
using KeyRepeatTuner.SystemAdapters.Interfaces;
using KeyRepeatTuner.Tests.TestUtilities.Fakes;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace KeyRepeatTuner.Tests.Services;

public class ProcessEventWatcherTests
{
    private readonly Mock<ILogger<ProcessEventWatcher>> _mockLogger = new();
    private readonly Mock<IProcessEventRouter> _mockRouter = new();
    private readonly Mock<IManagementEventWatcher> _mockStartWatcher = new();
    private readonly Mock<IManagementEventWatcher> _mockStopWatcher = new();
    private readonly Mock<IManagementEventWatcherFactory> _mockWatcherFactory = new();
    private readonly ProcessEventWatcher _watcher;

    public ProcessEventWatcherTests()
    {
        _mockWatcherFactory.Setup(f => f.Create(It.Is<string>(q => q.Contains("Start"))))
            .Returns(_mockStartWatcher.Object);
        _mockWatcherFactory.Setup(f => f.Create(It.Is<string>(q => q.Contains("Stop"))))
            .Returns(_mockStopWatcher.Object);

        _watcher = new ProcessEventWatcher(
            _mockLogger.Object,
            _mockWatcherFactory.Object,
            _mockRouter.Object,
            _ => new FakeEventArrivedEventArgs(9876));
    }

    [Fact]
    public void Configure_ShouldCreateWatchers_AndStartThem()
    {
        _watcher.Configure("starcraft");

        _mockStartWatcher.Verify(w => w.Start(), Times.Once);
        _mockStopWatcher.Verify(w => w.Start(), Times.Once);
    }

    [Fact]
    public void OnStartEventArrived_ShouldCallRouter()
    {
        _watcher.OnStartEventArrived(null!, "starcraft.exe");

        _mockRouter.Verify(r => r.OnProcessStarted(9876, "starcraft.exe"), Times.Once);
    }

    [Fact]
    public void OnStopEventArrived_ShouldCallRouter()
    {
        _watcher.OnStopEventArrived(null!, "starcraft.exe");

        _mockRouter.Verify(r => r.OnProcessStopped(9876, "starcraft.exe"), Times.Once);
    }

    [Fact]
    public void OnSettingsChanged_ShouldAddAndRemoveWatchers()
    {
        // Initial config
        var initialSettings = new AppSettings
        {
            ProcessNames = ["notepad"],
            KeyRepeat = new KeyRepeatSettings
            {
                Default = new KeyRepeatState { RepeatSpeed = 10, RepeatDelay = 750 },
                FastMode = new KeyRepeatState { RepeatSpeed = 20, RepeatDelay = 500 }
            }
        };

        _watcher.OnSettingsChanged(initialSettings);
        _mockStartWatcher.Verify(w => w.Start(), Times.Once);
        _mockStopWatcher.Verify(w => w.Start(), Times.Once);

        // Reconfigure with different process
        var updatedSettings = new AppSettings
        {
            ProcessNames = ["starcraft"],
            KeyRepeat = new KeyRepeatSettings
            {
                Default = new KeyRepeatState { RepeatSpeed = 15, RepeatDelay = 1000 },
                FastMode = new KeyRepeatState { RepeatSpeed = 5, RepeatDelay = 250 }
            }
        };

        _watcher.OnSettingsChanged(updatedSettings);

        _mockStartWatcher.Verify(w => w.Stop(), Times.Once);
        _mockStopWatcher.Verify(w => w.Stop(), Times.Once);
    }
}