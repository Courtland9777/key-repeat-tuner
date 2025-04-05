using System.Diagnostics;
using System.Security.Principal;
using Microsoft.Win32;
using Xunit;
using Xunit.Abstractions;

namespace KeyRepeatTuner.Tests.SystemLevel;

public class SystemLevelTests : IDisposable
{
    private readonly string _logFile = Path.Combine(Path.GetTempPath(), "kr_systemtest.log");
    private readonly ITestOutputHelper _testOutputHelper;
    private Process? _appProcess;

    public SystemLevelTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        Assert.True(IsAdministrator(), "Tests must run with administrator privileges.");
    }

    public void Dispose()
    {
        _appProcess?.Kill(true);
        _appProcess?.WaitForExit(5000);
        if (File.Exists(_logFile))
            File.Delete(_logFile);
    }

    [Fact]
    public async Task App_Applies_KeyRepeatSettings_When_WatchedProcessStarts()
    {
        var appPath = GetAppPath();
        _testOutputHelper.WriteLine($"Starting app from: {appPath}");

        var startInfo = new ProcessStartInfo(appPath)
        {
            RedirectStandardOutput = false,
            UseShellExecute = false,
            WorkingDirectory = Path.GetDirectoryName(appPath)!,
            EnvironmentVariables = { ["DOTNET_ENVIRONMENT"] = "Production" }
        };

        _appProcess = Process.Start(startInfo)!;
        Assert.NotNull(_appProcess);

        await Task.Delay(3000); // Let app initialize

        var originalSpeed = Registry.GetValue(@"HKEY_CURRENT_USER\Control Panel\Keyboard", "KeyboardSpeed", null);
        var originalDelay = Registry.GetValue(@"HKEY_CURRENT_USER\Control Panel\Keyboard", "KeyboardDelay", null);
        _testOutputHelper.WriteLine($"Original Speed={originalSpeed}, Delay={originalDelay}");

        Process? notepad = null;
        try
        {
            notepad = Process.Start("notepad.exe");
            Assert.NotNull(notepad);
            _testOutputHelper.WriteLine($"Started notepad.exe (PID: {notepad.Id})");

            await Task.Delay(5000); // Allow WMI event detection and settings to apply

            var newSpeed = Registry.GetValue(@"HKEY_CURRENT_USER\Control Panel\Keyboard", "KeyboardSpeed", null);
            var newDelay = Registry.GetValue(@"HKEY_CURRENT_USER\Control Panel\Keyboard", "KeyboardDelay", null);

            _testOutputHelper.WriteLine($"New Speed={newSpeed}, Delay={newDelay}");

            Assert.True(!Equals(originalSpeed, newSpeed) || !Equals(originalDelay, newDelay),
                $"Expected key repeat settings to change, but they did not. Speed={newSpeed}, Delay={newDelay}");
        }
        finally
        {
            if (notepad is { HasExited: false })
            {
                _testOutputHelper.WriteLine("Killing notepad.exe...");
                notepad.Kill(true);
                await notepad.WaitForExitAsync();
            }

            // Restore registry
            _testOutputHelper.WriteLine("Restoring original keyboard settings...");
            Registry.SetValue(@"HKEY_CURRENT_USER\Control Panel\Keyboard", "KeyboardSpeed", originalSpeed!);
            Registry.SetValue(@"HKEY_CURRENT_USER\Control Panel\Keyboard", "KeyboardDelay", originalDelay!);
        }
    }

    private static string GetAppPath()
    {
        const string exePath =
            @"C:\Users\Court\source\repos\KeyRepeatTuner\KeyRepeatTuner\bin\Debug\net8.0-windows\win-x64\KeyRepeatTuner.exe";
        if (!File.Exists(exePath))
            throw new FileNotFoundException("KeyRepeatTuner.exe not found at expected location", exePath);
        return exePath;
    }

    private static bool IsAdministrator()
    {
        using var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }
}