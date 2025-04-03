using System.Management;
using KeyRepeatTuner.Events;
using KeyRepeatTuner.Services;
using KeyRepeatTuner.SystemAdapters.Interfaces;
using KeyRepeatTuner.Tests.TestUtilities.Fakes;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace KeyRepeatTuner.Tests.Services;

public class ProcessEventWatcherTests
{
    private readonly Mock<ILogger<ProcessEventWatcher>> _mockLogger = new();
    private readonly Mock<IMediator> _mockMediator = new();
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
            _mockMediator.Object,
            TestAdapter);
        return;

        static IEventArrivedEventArgs TestAdapter(EventArrivedEventArgs _)
        {
            return new FakeEventArrivedEventArgs(9876);
        }
    }

    [Fact]
    public void Configure_ChangesWatcher_WhenNewProcessNameGiven()
    {
        _watcher.Configure("starcraft");
        _watcher.Configure("warcraft");

        _mockStartWatcher.Verify(w => w.Stop(), Times.Once);
        _mockStartWatcher.Verify(w => w.Dispose(), Times.Once);
        _mockStopWatcher.Verify(w => w.Stop(), Times.Once);
        _mockStopWatcher.Verify(w => w.Dispose(), Times.Once);
    }

    [Fact]
    public void Configure_DoesNothing_WhenProcessNameIsSame()
    {
        _watcher.Configure("notepad");
        _watcher.Configure("notepad");

        _mockStartWatcher.Verify(w => w.Start(), Times.Once);
        _mockStopWatcher.Verify(w => w.Start(), Times.Once);
    }

    [Fact]
    public void Dispose_CleansUpWatchers()
    {
        _watcher.Configure("test");
        _watcher.Dispose();

        _mockStartWatcher.Verify(w => w.Stop(), Times.AtLeastOnce);
        _mockStopWatcher.Verify(w => w.Dispose(), Times.AtLeastOnce);
    }

    [Fact]
    public void OnStartEventArrived_ShouldPublishProcessStarted()
    {
        const string processName = "dosbox.exe";

        _watcher.Configure("dosbox");
        _watcher.OnStartEventArrived(null!, processName);

        _mockMediator.Verify(m =>
            m.Publish(It.Is<ProcessStarted>(x =>
                x.ProcessId == 9876 &&
                x.ProcessName == processName), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void OnStopEventArrived_ShouldPublishProcessStopped()
    {
        const string processName = "quake.exe";

        _watcher.Configure("quake");
        _watcher.OnStopEventArrived(null!, processName); // pass process name explicitly

        _mockMediator.Verify(m =>
            m.Publish(It.Is<ProcessStopped>(x =>
                x.ProcessId == 9876 &&
                x.ProcessName == processName), It.IsAny<CancellationToken>()), Times.Once);
    }
}