using System.Diagnostics;

namespace StarCraftKeyManager.Tests;

public class PerformanceTests
{
    [Fact]
    public void Application_ShouldConsumeLowCPU()
    {
        using var process = Process.Start(new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = "run --project StarCraftKeyManager",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        });

        Assert.NotNull(process);
        process.WaitForExit(5000); // Let it run for 5 seconds

        var cpuUsage = GetCpuUsage(process);
        Assert.True(cpuUsage < 10, $"Expected CPU usage to be low, but it was {cpuUsage}%");

        process.Kill();
    }

    private static double GetCpuUsage(Process process)
    {
        return process.TotalProcessorTime.TotalMilliseconds / (Environment.ProcessorCount * 1000);
    }
}