using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using StarCraftKeyManager.Adapters;
using StarCraftKeyManager.Interfaces;
using StarCraftKeyManager.Models;
using StarCraftKeyManager.Services;
using Xunit;

namespace StarCraftKeyManager.Tests;

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
        await Task.Delay(1000);

        var memoryUsage = GetMemoryUsageInMb();

        Assert.True(memoryUsage < 50, $"Memory usage is too high: {memoryUsage}MB");
    }

    [Fact]
    public async Task ProcessEventOccurred_ShouldHandleHighVolumeProcessEvents_Efficiently()
    {
        await _processMonitorService.StartAsync(CancellationToken.None);
        await Task.Delay(500);

        for (var i = 0; i < 1000; i++)
        {
            _mockProcessEventWatcher.Raise(
                w => w.ProcessEventOccurred += null,
                new ProcessEventArgs(4688, i, "starcraft.exe")
            );

            _mockProcessEventWatcher.Raise(
                w => w.ProcessEventOccurred += null,
                new ProcessEventArgs(4689, i, "starcraft.exe")
            );
        }

        await Task.Delay(500);

        var cpuUsage = GetCpuUsage();
        var memoryUsage = GetMemoryUsageInMb();

        Assert.True(cpuUsage < 15, $"High CPU usage detected: {cpuUsage}%");
        Assert.True(memoryUsage < 75, $"High memory usage detected: {memoryUsage}MB");
    }

    [Fact]
    public async Task StartAndStop_ShouldNotCauseMemoryLeaks_AfterMultipleCycles()
    {
        var initialMemoryUsage = Process.GetCurrentProcess().PrivateMemorySize64;

        for (var i = 0; i < 10; i++)
        {
            await _processMonitorService.StartAsync(CancellationToken.None);
            await Task.Delay(200);
            await _processMonitorService.StopAsync(CancellationToken.None);
        }

        var finalMemoryUsage = Process.GetCurrentProcess().PrivateMemorySize64;

        Assert.True(finalMemoryUsage - initialMemoryUsage < 5 * 1024 * 1024, // 5MB threshold
            $"Possible memory leak detected. Initial: {initialMemoryUsage / 1024 / 1024}MB, Final: {finalMemoryUsage / 1024 / 1024}MB");
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