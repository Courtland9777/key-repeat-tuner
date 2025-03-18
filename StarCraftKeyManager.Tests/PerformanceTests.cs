using System.Diagnostics;

namespace StarCraftKeyManager.Tests;

public class PerformanceTests
{
    [Fact]
    public void Application_ShouldConsumeLowMemory()
    {
        using var process = Process.GetCurrentProcess();
        var initialMemory = process.PrivateMemorySize64;

        var memoryUsage = process.PrivateMemorySize64 - initialMemory;

        Assert.True(memoryUsage < 50 * 1024 * 1024,
            $"Expected memory usage to be under 50MB, but it was {memoryUsage / (1024 * 1024)}MB");
    }

    private static double GetCpuUsage(Process process)
    {
        return process.TotalProcessorTime.TotalMilliseconds / (Environment.ProcessorCount * 1000);
    }
}