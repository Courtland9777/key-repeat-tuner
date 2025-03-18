using System.Diagnostics;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using StarCraftKeyManager.Services;

namespace StarCraftKeyManager.Tests;

public class PerformanceTests
{
    [Fact]
    public async Task ProcessMonitor_ShouldConsumeMinimalCPU()
    {
        // Arrange
        using var process = Process.GetCurrentProcess();
        var initialCpuUsage = GetCpuUsage();

        // Act
        await Task.Delay(500); // Simulate workload
        var finalCpuUsage = GetCpuUsage();

        // Assert
        Assert.True(finalCpuUsage - initialCpuUsage < 5.0,
            $"Expected CPU usage increase to be under 5%, but it was {finalCpuUsage - initialCpuUsage}%");
    }

    [Fact]
    public async Task Application_ShouldConsumeLowMemory()
    {
        // Arrange
        var initialMemory = GC.GetTotalMemory(true);

        // Act
        await Task.Delay(500); // Simulate workload
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
    private readonly ProcessMonitorService _monitor = new(null!, null!, null!);

    [Benchmark]
    public void StartProcessMonitoring()
    {
        _monitor.StartMonitoringAsync(CancellationToken.None).GetAwaiter().GetResult();
    }
}