using System.Diagnostics;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using StarCraftKeyManager.Interfaces;
using StarCraftKeyManager.Models;
using StarCraftKeyManager.Services;

namespace StarCraftKeyManager.Tests;

public class PerformanceTests
{
    private readonly ProcessMonitorService _processMonitorService;

    public PerformanceTests()
    {
        Mock<ILogger<ProcessMonitorService>> mockLogger = new();
        Mock<IOptionsMonitor<AppSettings>> mockOptionsMonitor = new();
        Mock<IProcessEventWatcher> mockProcessEventWatcher = new();

        var mockAppSettings = new AppSettings
        {
            ProcessMonitor = new ProcessMonitorSettings { ProcessName = "starcraft.exe" },
            KeyRepeat = new KeyRepeatSettings
            {
                Default = new KeyRepeatState { RepeatSpeed = 31, RepeatDelay = 1000 },
                FastMode = new KeyRepeatState { RepeatSpeed = 20, RepeatDelay = 500 }
            }
        };

        mockOptionsMonitor.Setup(o => o.CurrentValue).Returns(mockAppSettings);

        _processMonitorService = new ProcessMonitorService(
            mockLogger.Object,
            mockOptionsMonitor.Object,
            mockProcessEventWatcher.Object
        );
    }

    [Fact]
    public async Task ProcessMonitor_ShouldConsumeMinimalCPU()
    {
        // Arrange
        using var process = Process.GetCurrentProcess();
        var initialCpuUsage = GetCpuUsage();

        // Act
        await _processMonitorService.StartAsync(CancellationToken.None);
        await Task.Delay(500); // Simulate processing
        var finalCpuUsage = GetCpuUsage();

        // Assert
        Assert.True(finalCpuUsage - initialCpuUsage < 5.0,
            $"Expected CPU usage increase to be under 5%, but it was {finalCpuUsage - initialCpuUsage}%");
    }

    [Fact]
    public async Task ProcessMonitor_ShouldConsumeLowMemory()
    {
        // Arrange
        var initialMemory = GC.GetTotalMemory(true);

        // Act
        await _processMonitorService.StartAsync(CancellationToken.None);
        await Task.Delay(500); // Simulate processing
        var finalMemory = GC.GetTotalMemory(true);

        // Assert
        Assert.True(finalMemory - initialMemory < 50 * 1024 * 1024, // 50 MB
            $"Expected memory usage increase to be under 50MB, but it was {(finalMemory - initialMemory) / (1024 * 1024)}MB");
    }

    [Fact]
    public async Task Benchmark_ProcessMonitorPerformance()
    {
        var summary = await Task.Run(() => BenchmarkRunner.Run<ProcessMonitorBenchmark>());
        Assert.NotNull(summary);
    }

    private static double GetCpuUsage()
    {
        using var process = Process.GetCurrentProcess();
        return process.TotalProcessorTime.TotalMilliseconds / Environment.ProcessorCount;
    }
}

[MemoryDiagnoser]
public class ProcessMonitorBenchmark
{
    private readonly ProcessMonitorService _monitor;

    public ProcessMonitorBenchmark()
    {
        var mockLogger = new Mock<ILogger<ProcessMonitorService>>();
        var mockOptionsMonitor = new Mock<IOptionsMonitor<AppSettings>>();
        var mockEventWatcher = new Mock<IProcessEventWatcher>();

        var mockAppSettings = new AppSettings
        {
            ProcessMonitor = new ProcessMonitorSettings { ProcessName = "starcraft.exe" },
            KeyRepeat = new KeyRepeatSettings
            {
                Default = new KeyRepeatState { RepeatSpeed = 31, RepeatDelay = 1000 },
                FastMode = new KeyRepeatState { RepeatSpeed = 20, RepeatDelay = 500 }
            }
        };

        mockOptionsMonitor.Setup(o => o.CurrentValue).Returns(mockAppSettings);
        _monitor = new ProcessMonitorService(mockLogger.Object, mockOptionsMonitor.Object, mockEventWatcher.Object);
    }

    [Benchmark]
    public void StartProcessMonitoring()
    {
        _monitor.StartMonitoringAsync(CancellationToken.None).GetAwaiter().GetResult();
    }

    [Benchmark]
    public static void MeasureMemoryUsage()
    {
        var allocated = GC.GetTotalMemory(true);
        Assert.True(allocated < 50_000_000, $"Memory usage too high: {allocated} bytes.");
    }

    [Benchmark]
    public void MonitorProcessPerformance()
    {
        var stopwatch = Stopwatch.StartNew();
        _monitor.StartMonitoringAsync(CancellationToken.None).GetAwaiter().GetResult();
        stopwatch.Stop();
        Assert.True(stopwatch.ElapsedMilliseconds < 500,
            $"Process monitoring took too long: {stopwatch.ElapsedMilliseconds}ms.");
    }
}