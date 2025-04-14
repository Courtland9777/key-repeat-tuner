using System.Diagnostics;
using System.Security.Principal;
using KeyRepeatTuner.SystemAdapters.Interfaces;
using KeyRepeatTuner.SystemAdapters.Keyboard;
using Microsoft.Win32;
using Xunit;
using Xunit.Abstractions;

namespace KeyRepeatTuner.SystemTests.SystemLevel;

public class SystemLevelTests : IDisposable
{
    private readonly string _logFile =
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", "process_monitor.log");

    private readonly string? _originalDelay;
    private readonly string? _originalSpeed;
    private readonly IKeyboardRegistryReader _registryReader;
    private readonly ITestOutputHelper _testOutputHelper;
    private Process? _appProcess;

    public SystemLevelTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _registryReader = new KeyboardRegistryReader();

        Assert.True(IsAdministrator(), "Tests must run with administrator privileges.");

        _originalSpeed = _registryReader.GetRepeatSpeed();
        _originalDelay = _registryReader.GetRepeatDelay();
    }

    public void Dispose()
    {
        if (_appProcess is { HasExited: false })
            _appProcess.Kill(true);

        _appProcess?.WaitForExit(5000);

        if (File.Exists(_logFile))
        {
            var logs = File.ReadAllText(_logFile);
            _testOutputHelper.WriteLine("Captured App Log Output:");
            _testOutputHelper.WriteLine(logs);
        }

        if (_originalSpeed is not null)
            Registry.SetValue(@"HKEY_CURRENT_USER\Control Panel\Keyboard", "KeyboardSpeed", _originalSpeed);

        if (_originalDelay is not null)
            Registry.SetValue(@"HKEY_CURRENT_USER\Control Panel\Keyboard", "KeyboardDelay", _originalDelay);

        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task App_ShouldApplyKeyRepeatSettings_When_WatchedCmdProcessRuns()
    {
        StartKeyRepeatApp();
        await Task.Delay(3000);

        var originalSpeed = _registryReader.GetRepeatSpeed();
        var originalDelay = _registryReader.GetRepeatDelay();
        _testOutputHelper.WriteLine($"Original Speed={originalSpeed}, Delay={originalDelay}");

        var cmd = StartWatchedCmdProcess();
        await Task.Delay(3000);

        var newSpeed = _registryReader.GetRepeatSpeed();
        var newDelay = _registryReader.GetRepeatDelay();
        _testOutputHelper.WriteLine($"New Speed={newSpeed}, Delay={newDelay}");

        Assert.False(string.IsNullOrWhiteSpace(newSpeed) && string.IsNullOrWhiteSpace(newDelay),
            "Failed to read keyboard settings from registry. Are they missing or malformed?");

        Assert.Equal("31", newSpeed); // FastMode.Speed
        Assert.Equal("2", newDelay); // 500ms = code 2

        await cmd.WaitForExitAsync();
    }

    [Fact]
    public async Task App_ShouldRevertToDefaultSettings_When_WatchedProcessExits()
    {
        StartKeyRepeatApp();
        await Task.Delay(3000);

        var cmd = StartWatchedCmdProcess();
        await cmd.WaitForExitAsync();
        await Task.Delay(3000);

        var speed = _registryReader.GetRepeatSpeed();
        var delay = _registryReader.GetRepeatDelay();

        _testOutputHelper.WriteLine($"Final Speed={speed}, Delay={delay}");

        Assert.False(string.IsNullOrWhiteSpace(speed) && string.IsNullOrWhiteSpace(delay),
            "Failed to read keyboard settings from registry. Are they missing or malformed?");

        Assert.Equal("20", speed); // Default.Speed
        Assert.Equal("3", delay); // 750ms = code 3
    }

    [Fact]
    public async Task App_ShouldRespondToMultipleProcesses_WhenTheyStartAndStop()
    {
        StartKeyRepeatApp();
        await Task.Delay(5000);

        _testOutputHelper.WriteLine(
            $"Start → Speed={_registryReader.GetRepeatSpeed()}, Delay={_registryReader.GetRepeatDelay()}");

        var firstCmdProcess = StartWatchedCmdProcess();
        await Task.Delay(3000);
        var secondCmdProcess = StartWatchedCmdProcess();
        await Task.Delay(3000);

        var speedDuringFastMode = _registryReader.GetRepeatSpeed();
        var delayDuringFastMode = _registryReader.GetRepeatDelay();

        _testOutputHelper.WriteLine($"FastMode → Speed={speedDuringFastMode}, Delay={delayDuringFastMode}");

        await firstCmdProcess.WaitForExitAsync();
        await secondCmdProcess.WaitForExitAsync();
        await Task.Delay(3000);

        var speedAfterExit = _registryReader.GetRepeatSpeed();
        var delayAfterExit = _registryReader.GetRepeatDelay();

        _testOutputHelper.WriteLine($"PostExit → Speed={speedAfterExit}, Delay={delayAfterExit}");

        Assert.NotEqual(speedAfterExit, speedDuringFastMode);
        Assert.NotEqual(delayAfterExit, delayDuringFastMode);
    }

    private void StartKeyRepeatApp(IEnumerable<KeyValuePair<string, string>>? configOverrides = null)
    {
        var startInfo = new ProcessStartInfo(GetAppPath())
        {
            RedirectStandardOutput = false,
            UseShellExecute = false,
            WorkingDirectory = Path.GetDirectoryName(GetAppPath())!
        };

        if (configOverrides is not null)
            foreach (var kv in configOverrides)
            {
                var envKey = kv.Key.Replace(":", "__");
                startInfo.Environment[envKey] = kv.Value;
            }

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
            @"C:\Users\Court\source\repos\KeyRepeatTuner\KeyRepeatTuner\bin\Debug\net8.0-windows\win-x64\KeyRepeatTuner.exe";
        if (!File.Exists(fallbackPath))
            throw new FileNotFoundException("KeyRepeatTuner.exe not found at expected location", fallbackPath);

        return fallbackPath;
    }

    private static bool IsAdministrator()
    {
        using var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }
}