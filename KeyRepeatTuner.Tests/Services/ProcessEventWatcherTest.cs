using KeyRepeatTuner.Configuration;
using KeyRepeatTuner.Configuration.ValueObjects;
using KeyRepeatTuner.Monitoring.Interfaces;
using KeyRepeatTuner.Monitoring.Services;
using KeyRepeatTuner.SystemAdapters.Interfaces;
using KeyRepeatTuner.Tests.TestUtilities.Extensions;
using KeyRepeatTuner.Tests.TestUtilities.Fakes;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace KeyRepeatTuner.Tests.Services;

public class ProcessEventWatcherTests
{
    private readonly KeyRepeatSettings _defaultKeyRepeat = new()
    {
        Default = new KeyRepeatState { RepeatSpeed = 20, RepeatDelay = 750 },
        FastMode = new KeyRepeatState { RepeatSpeed = 10, RepeatDelay = 500 }
    };

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
    public void OnStartEventArrived_ShouldCallRouter()
    {
        // Act
        _watcher.OnStartEventArrived(9876, "starcraft.exe");

        // Assert
        _mockRouter.Verify(r => r.OnProcessStarted(9876, "starcraft.exe"), Times.Once);
    }

    [Fact]
    public void OnStopEventArrived_ShouldCallRouter()
    {
        // Act
        _watcher.OnStopEventArrived(9876, "starcraft.exe");

        // Assert
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

    [Fact]
    public void OnSettingsChanged_ShouldStartAndStopCorrectWatchers()
    {
        // Arrange
        var createdWatchers = new List<string>();
        var disposedWatchers = new List<string>();

        _mockWatcherFactory.Setup(f => f.Create(It.IsAny<string>()))
            .Returns<string>(query =>
            {
                var watcher = new Mock<IManagementEventWatcher>();
                watcher.Setup(w => w.Start()).Callback(() => createdWatchers.Add(query));
                watcher.Setup(w => w.Dispose()).Callback(() => disposedWatchers.Add(query));
                return watcher.Object;
            });

        var appSettings1 = new AppSettings
        {
            ProcessNames = [new ProcessName("notepad"), new ProcessName("mspaint")],
            KeyRepeat = _defaultKeyRepeat
        };

        var appSettings2 = new AppSettings
        {
            ProcessNames =
                [new ProcessName("notepad"), new ProcessName("starcraft")], // mspaint removed, starcraft added
            KeyRepeat = _defaultKeyRepeat
        };

        // Initial config
        _watcher.OnSettingsChanged(appSettings1);
        createdWatchers.Clear(); // we only care about the diff now

        // Act
        _watcher.OnSettingsChanged(appSettings2);

        // Assert
        Assert.Contains("starcraft.exe", string.Join(";", createdWatchers));
        Assert.Contains("mspaint.exe", string.Join(";", disposedWatchers));
        Assert.DoesNotContain("notepad.exe", string.Join(";", disposedWatchers));
    }

    [Fact]
    public void Start_ShouldLaunchAllWatchersAndLogFailures()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<ProcessEventWatcher>>();

        _mockWatcherFactory.Setup(f => f.Create(It.Is<string>(q => q.Contains("bad.exe"))))
            .Throws(new InvalidOperationException("WMI error"));

        _mockWatcherFactory.Setup(f => f.Create(It.Is<string>(q => q.Contains("notepad.exe"))))
            .Returns(Mock.Of<IManagementEventWatcher>);

        var watcher = new ProcessEventWatcher(
            mockLogger.Object,
            _mockWatcherFactory.Object,
            _mockRouter.Object);

        watcher.OnSettingsChanged(new AppSettings
        {
            ProcessNames = [new ProcessName("notepad"), new ProcessName("bad")],
            KeyRepeat = _defaultKeyRepeat
        });

        // Act
        watcher.Start();

        // Assert
        mockLogger.VerifyLogContains(LogLevel.Error, "Failed to start WMI process watchers for process.");
    }

    [Fact]
    public void Stop_ShouldDisposeAllWatchers_AndLogInfo()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<ProcessEventWatcher>>();
        var disposedWatchers = new List<string>();

        _mockWatcherFactory.Setup(f => f.Create(It.IsAny<string>()))
            .Returns<string>(query =>
            {
                var watcher = new Mock<IManagementEventWatcher>();
                watcher.Setup(w => w.Start()); // no-op
                watcher.Setup(w => w.Dispose()).Callback(() => disposedWatchers.Add(query));
                return watcher.Object;
            });

        var watcher = new ProcessEventWatcher(
            mockLogger.Object,
            _mockWatcherFactory.Object,
            _mockRouter.Object);

        watcher.OnSettingsChanged(new AppSettings
        {
            ProcessNames = [new ProcessName("notepad"), new ProcessName("starcraft")],
            KeyRepeat = _defaultKeyRepeat
        });

        watcher.Start(); // start so the watchers exist

        // Act
        watcher.Stop();

        // Assert
        Assert.Equal(2, disposedWatchers.Count(w => w.Contains("notepad.exe")));
        Assert.Equal(2, disposedWatchers.Count(w => w.Contains("starcraft.exe")));

        mockLogger.VerifyLogContains(LogLevel.Information, "WMI process watchers stopped.");
    }

    [Fact]
    public void Dispose_ShouldCallStop_AndDisposeAllWatchers()
    {
        // Arrange
        var disposedWatchers = new List<string>();

        _mockWatcherFactory.Setup(f => f.Create(It.IsAny<string>()))
            .Returns<string>(query =>
            {
                var watcher = new Mock<IManagementEventWatcher>();
                watcher.Setup(w => w.Start());
                watcher.Setup(w => w.Dispose()).Callback(() => disposedWatchers.Add(query));
                return watcher.Object;
            });

        var watcher = new ProcessEventWatcher(
            _mockLogger.Object,
            _mockWatcherFactory.Object,
            _mockRouter.Object);

        watcher.OnSettingsChanged(new AppSettings
        {
            ProcessNames = [new ProcessName("notepad")],
            KeyRepeat = _defaultKeyRepeat
        });

        watcher.Start();

        // Act
        watcher.Dispose();

        // Assert
        Assert.Equal(2, disposedWatchers.Count); // notepad.exe start + stop
        Assert.All(disposedWatchers, q => Assert.Contains("notepad.exe", q));
    }
}