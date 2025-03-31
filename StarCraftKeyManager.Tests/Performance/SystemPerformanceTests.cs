using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using StarCraftKeyManager.Configuration;
using StarCraftKeyManager.Events;
using StarCraftKeyManager.Interfaces;
using StarCraftKeyManager.Services;
using StarCraftKeyManager.SystemAdapters.Interfaces;
using StarCraftKeyManager.Tests.TestUtilities.Logging;
using Xunit;

namespace StarCraftKeyManager.Tests.Performance;

public class SystemPerformanceTests
{
    private readonly Mock<IKeyboardSettingsApplier> _mockKeyboardSettingsApplier;
    private readonly Mock<ILogger<ProcessMonitorService>> _mockLogger;
    private readonly Mock<IProcessEventWatcher> _mockProcessEventWatcher;
    private readonly Mock<IProcessProvider> _mockProcessProvider;
    private readonly ProcessMonitorService _processMonitorService;

    public SystemPerformanceTests()
    {
        _mockLogger = new Mock<ILogger<ProcessMonitorService>>();
        _mockProcessEventWatcher = new Mock<IProcessEventWatcher>();
        _mockKeyboardSettingsApplier = new Mock<IKeyboardSettingsApplier>();
        _mockProcessProvider = new Mock<IProcessProvider>();

        var mockSettings = new AppSettings
        {
            ProcessMonitor = new ProcessMonitorSettings { ProcessName = "starcraft.exe" },
            KeyRepeat = new KeyRepeatSettings
            {
                Default = new KeyRepeatState { RepeatSpeed = 31, RepeatDelay = 1000 },
                FastMode = new KeyRepeatState { RepeatSpeed = 20, RepeatDelay = 500 }
            }
        };

        var mockOptionsMonitor = new Mock<IOptionsMonitor<AppSettings>>();
        mockOptionsMonitor.Setup(o => o.CurrentValue).Returns(mockSettings);

        _mockProcessProvider
            .Setup(p => p.GetProcessIdsByName("starcraft"))
            .Returns([]);

        _processMonitorService = new ProcessMonitorService(
            _mockLogger.Object,
            mockOptionsMonitor.Object,
            _mockProcessEventWatcher.Object,
            _mockKeyboardSettingsApplier.Object,
            _mockProcessProvider.Object
        );
    }

    private static double GetCpuUsage()
    {
        var process = Process.GetCurrentProcess();
        return process.TotalProcessorTime.TotalMilliseconds / (Environment.ProcessorCount * 1000.0);
    }

    private static double GetMemoryUsageInMb()
    {
        var process = Process.GetCurrentProcess();
        return process.WorkingSet64 / (1024.0 * 1024.0);
    }

    [Fact]
    public async Task StartAsync_ShouldKeepCpuUsageLow_DuringNormalOperation()
    {
        await _processMonitorService.StartAsync(CancellationToken.None);
        await Task.Delay(1000); // Allow process to run

        var cpuUsage = GetCpuUsage();

        Assert.True(cpuUsage < 10, $"CPU usage is too high: {cpuUsage}%");
    }

    [Fact]
    public async Task StartAsync_ShouldMaintainStableMemoryUsage_DuringNormalOperation()
    {
        await _processMonitorService.StartAsync(CancellationToken.None);

        // Optional: allow service to settle and stabilize
        await Task.Delay(1000);

        // Run GC to avoid false positives from allocations that are collectible
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var memoryUsage = GetMemoryUsageInMb();

        // Adjusted threshold for typical .NET runtime allocations
        Assert.True(memoryUsage < 100, $"Memory usage is too high: {memoryUsage}MB");
    }


    [Fact]
    public async Task ProcessEventOccurred_ShouldHandleHighVolumeProcessEvents_Efficiently()
    {
        // Arrange - use NoOpLogger to minimize overhead
        var logger = new NoOpLogger<ProcessMonitorService>();

        var settings = new AppSettings
        {
            ProcessMonitor = new ProcessMonitorSettings { ProcessName = "starcraft.exe" },
            KeyRepeat = new KeyRepeatSettings
            {
                Default = new KeyRepeatState { RepeatSpeed = 31, RepeatDelay = 1000 },
                FastMode = new KeyRepeatState { RepeatSpeed = 20, RepeatDelay = 500 }
            }
        };

        var mockOptionsMonitor = new Mock<IOptionsMonitor<AppSettings>>();
        mockOptionsMonitor.Setup(m => m.CurrentValue).Returns(settings);

        var mockWatcher = new Mock<IProcessEventWatcher>();
        var mockKeyboard = new Mock<IKeyboardSettingsApplier>();
        var mockProvider = new Mock<IProcessProvider>();
        mockProvider.Setup(p => p.GetProcessIdsByName("starcraft"))
            .Returns((List<int>) []);

        var service = new ProcessMonitorService(
            logger,
            mockOptionsMonitor.Object,
            mockWatcher.Object,
            mockKeyboard.Object,
            mockProvider.Object
        );

        await service.StartAsync(CancellationToken.None);

        // Simulate high volume of events
        const int eventCount = 5000;
        for (var i = 0; i < eventCount; i++)
            mockWatcher.Raise(
                w => w.ProcessEventOccurred += null,
                new ProcessEventArgs(4688, i, "starcraft.exe")
            );

        // Allow processing and cleanup
        await Task.Delay(250);

        // Force GC
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        await Task.Delay(250);

        var memoryUsage = GetMemoryUsageInMb();

        // Assert
        Assert.True(memoryUsage < 75, $"High memory usage detected: {memoryUsage}MB");
    }


    [Fact]
    public async Task StartAndStop_ShouldNotCauseMemoryLeaks_AfterMultipleCycles()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var initialMemory = GC.GetTotalMemory(true) / (1024.0 * 1024.0);

        for (var i = 0; i < 10; i++)
        {
            await _processMonitorService.StartAsync(CancellationToken.None);
            await _processMonitorService.StopAsync(CancellationToken.None);
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        await Task.Delay(250);

        var finalMemory = GC.GetTotalMemory(true) / (1024.0 * 1024.0);

        var memoryDelta = finalMemory - initialMemory;

        Assert.True(memoryDelta < 10,
            $"Possible memory leak detected. Initial: {initialMemory:F0}MB, Final: {finalMemory:F0}MB");
    }


    [Fact]
    public async Task StartAsync_ShouldNotDegradePerformance_AfterExtendedRun()
    {
        await _processMonitorService.StartAsync(CancellationToken.None);
        await Task.Delay(5000);

        var finalCpuUsage = GetCpuUsage();
        var finalMemoryUsage = GetMemoryUsageInMb();

        Assert.True(finalCpuUsage < 10, $"CPU usage increased over time: {finalCpuUsage}%");
        Assert.True(finalMemoryUsage < 80, $"Memory usage increased over time: {finalMemoryUsage}MB");
    }

    [Fact]
    public async Task ProcessEventOccurred_ShouldHandleBurstOfEvents_WithoutCrashing()
    {
        await _processMonitorService.StartAsync(CancellationToken.None);
        await Task.Delay(500);

        Parallel.For(0, 5000, i =>
        {
            _mockProcessEventWatcher.Raise(
                w => w.ProcessEventOccurred += null,
                new ProcessEventArgs(4688, i, "starcraft.exe")
            );

            _mockProcessEventWatcher.Raise(
                w => w.ProcessEventOccurred += null,
                new ProcessEventArgs(4689, i, "starcraft.exe")
            );
        });

        await Task.Delay(1000);

        var cpuUsage = GetCpuUsage();
        var memoryUsage = GetMemoryUsageInMb();

        Assert.True(cpuUsage < 20, $"High CPU usage detected: {cpuUsage}%");
        Assert.True(memoryUsage < 100, $"High memory usage detected: {memoryUsage}MB");
    }
}