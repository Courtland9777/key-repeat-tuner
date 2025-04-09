using System.Diagnostics;
using System.Security.Principal;
using Microsoft.Win32;
using Xunit;
using Xunit.Abstractions;

namespace KeyRepeatTuner.Tests.SystemLevel;

public class SystemLevelTests : IDisposable
{
    private readonly string _logFile = Path.Combine(Path.GetTempPath(), "kr_systemtest.log");
    private readonly object? _originalDelay;
    private readonly object? _originalSpeed;
    private readonly ITestOutputHelper _testOutputHelper;
    private Process? _appProcess;

    public SystemLevelTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        Assert.True(IsAdministrator(), "Tests must run with administrator privileges.");

        _originalSpeed = Registry.GetValue(@"HKEY_CURRENT_USER\\Control Panel\\Keyboard", "KeyboardSpeed", null);
        _originalDelay = Registry.GetValue(@"HKEY_CURRENT_USER\\Control Panel\\Keyboard", "KeyboardDelay", null);
    }

    public void Dispose()
    {
        if (_appProcess is { HasExited: false })
            _appProcess.Kill(true);

        _appProcess?.WaitForExit(5000);

        if (File.Exists(_logFile))
        {
            var logs = File.ReadAllText(_logFile);
            _testOutputHelper.WriteLine("Captured Logs:");
            _testOutputHelper.WriteLine(logs);
            File.Delete(_logFile);
        }

        if (_originalSpeed is not null)
            Registry.SetValue(@"HKEY_CURRENT_USER\\Control Panel\\Keyboard", "KeyboardSpeed", _originalSpeed);

        if (_originalDelay is not null)
            Registry.SetValue(@"HKEY_CURRENT_USER\\Control Panel\\Keyboard", "KeyboardDelay", _originalDelay);
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task App_ShouldApplyKeyRepeatSettings_When_WatchedCmdProcessRuns()
    {
        StartKeyRepeatApp();
        await Task.Delay(3000);

        var originalSpeed = GetKeyboardSpeed();
        var originalDelay = GetKeyboardDelay();
        _testOutputHelper.WriteLine($"Original Speed={originalSpeed}, Delay={originalDelay}");

        var cmd = StartWatchedCmdProcess();
        await Task.Delay(3000);

        var newSpeed = GetKeyboardSpeed();
        var newDelay = GetKeyboardDelay();
        _testOutputHelper.WriteLine($"New Speed={newSpeed}, Delay={newDelay}");

        await cmd.WaitForExitAsync();

        Assert.True(!Equals(originalSpeed, newSpeed) || !Equals(originalDelay, newDelay),
            $"Expected key repeat settings to change. Speed={newSpeed}, Delay={newDelay}");
    }

    [Fact]
    public async Task App_ShouldRevertToDefaultSettings_When_WatchedProcessExits()
    {
        StartKeyRepeatApp();
        await Task.Delay(3000);

        var cmd = StartWatchedCmdProcess();
        await Task.Delay(3000);
        await cmd.WaitForExitAsync();

        var speed = GetKeyboardSpeed();
        var delay = GetKeyboardDelay();

        _testOutputHelper.WriteLine($"Final Speed={speed}, Delay={delay}");

        Assert.Equal("20", speed); // Default = 31
        Assert.Equal("3", delay); // 1000ms = delay code 3
    }

    private void StartKeyRepeatApp()
    {
        var startInfo = new ProcessStartInfo(GetAppPath())
        {
            RedirectStandardOutput = false,
            UseShellExecute = false,
            WorkingDirectory = Path.GetDirectoryName(GetAppPath())!
        };

        _appProcess = Process.Start(startInfo)!;
        Assert.NotNull(_appProcess);
    }

    private static Process StartWatchedCmdProcess()
    {
        return Process.Start(new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = "/c ping 127.0.0.1 -n 3 >nul",
            CreateNoWindow = true,
            UseShellExecute = false
        })!;
    }

    private static string GetAppPath()
    {
        var customPath = Environment.GetEnvironmentVariable("KR_APP_PATH");
        if (!string.IsNullOrWhiteSpace(customPath) && File.Exists(customPath))
            return customPath;

        const string fallbackPath =
            @"C:\\Users\\Court\\source\\repos\\KeyRepeatTuner\\KeyRepeatTuner\\bin\\Debug\\net8.0-windows\\win-x64\\KeyRepeatTuner.exe";
        if (!File.Exists(fallbackPath))
            throw new FileNotFoundException("KeyRepeatTuner.exe not found at expected location", fallbackPath);

        return fallbackPath;
    }

    private static string? GetKeyboardSpeed()
    {
        return Registry.GetValue(@"HKEY_CURRENT_USER\Control Panel\Keyboard", "KeyboardSpeed", null)?.ToString();
    }

    private static string? GetKeyboardDelay()
    {
        return Registry.GetValue(@"HKEY_CURRENT_USER\Control Panel\Keyboard", "KeyboardDelay", null)?.ToString();
    }


    private static bool IsAdministrator()
    {
        using var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }
}