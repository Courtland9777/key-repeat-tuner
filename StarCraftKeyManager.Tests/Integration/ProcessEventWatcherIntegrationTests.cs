using System.Diagnostics.Eventing.Reader;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using StarCraftKeyManager.Configuration;
using StarCraftKeyManager.Interfaces;
using StarCraftKeyManager.Models;
using StarCraftKeyManager.Services;

namespace StarCraftKeyManager.Tests.Integration;

public class ProcessEventWatcherIntegrationTestsOld
{
    private readonly Mock<ILogger<ProcessEventWatcher>> _mockLogger = new();
    private readonly Mock<IOptionsMonitor<AppSettings>> _mockOptionsMonitor = new();
    private readonly Mock<IEventLogQueryBuilder> _mockQueryBuilder = new();
    private readonly Mock<IEventWatcherFactory> _mockWatcherFactory = new();


    public ProcessEventWatcherIntegrationTestsOld()
    {
        _mockOptionsMonitor.Setup(m => m.CurrentValue).Returns(new AppSettings
        {
            KeyRepeat = new KeyRepeatSettings
            {
                Default = new KeyRepeatState { RepeatDelay = 1000, RepeatSpeed = 31 },
                FastMode = new KeyRepeatState { RepeatDelay = 500, RepeatSpeed = 20 }
            },
            ProcessMonitor = new ProcessMonitorSettings { ProcessName = "starcraft.exe" }
        });

        _mockQueryBuilder.Setup(b => b.BuildQuery())
            .Returns(new EventLogQuery("Security", PathType.LogName, "MockQuery"));
    }
}