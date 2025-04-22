using System.Diagnostics;
using System.Security.Principal;
using KeyRepeatTuner.SystemAdapters.Interfaces;
using KeyRepeatTuner.SystemAdapters.Keyboard;
using KeyRepeatTuner.SystemTests.TestUtilities.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace KeyRepeatTuner.SystemTests.SystemLevel;

[Collection("SystemTests")]
public class SettingsReloadSystemTests : IDisposable
{
    private readonly string _logFile;
    private readonly string? _originalSettings;
    private readonly ITestOutputHelper _output;
    private readonly IKeyboardRegistryReader _reader;
    private readonly string _settingsPath;

    public SettingsReloadSystemTests(ITestOutputHelper output)
    {
        _output = output;
        _reader = new KeyboardRegistryReader();

        _settingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "KeyRepeatTuner", "appsettings.json");

        _logFile = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "logs", "process_monitor.log");

        _originalSettings = File.Exists(_settingsPath) ? File.ReadAllText(_settingsPath) : null;

        Assert.True(IsAdministrator(), "Test must be run as administrator.");
    }

    public void Dispose()
    {
        if (_originalSettings is not null)
            File.WriteAllText(_settingsPath, _originalSettings);

        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task App_ShouldApplyNewSettings_When_AppSettingsJsonIsModified()
    {
        // 🔄 Overwrite live appsettings.json with quoted string values
        var updatedJson = """
                          {
                            "ProcessNames": [ "cmd" ],
                            "KeyRepeat": {
                              "Default": {
                                "RepeatSpeed": "10",
                                "RepeatDelay": "250"
                              },
                              "FastMode": {
                                "RepeatSpeed": "15",
                                "RepeatDelay": "500"
                              }
                            }
                          }
                          """;

        var tempPath = Path.GetTempFileName();
        File.WriteAllText(tempPath, updatedJson);
        File.Replace(tempPath, _settingsPath, null);
        _output.WriteLine("🔄 Settings updated on disk.");

        using var app = new KeyRepeatAppRunner(_logFile);
        app.Start();

        await Task.Delay(3000);

        using var cmd = StartCmdProcess();
        await Task.Delay(4000); // let app detect process and apply FastMode

        var speed = _reader.GetRepeatSpeed();
        var delay = _reader.GetRepeatDelay();

        _output.WriteLine("Expected: Speed=15, Delay=2");
        _output.WriteLine($"Observed: Speed={speed}, Delay={delay}");

        if (File.Exists(_logFile))
        {
            _output.WriteLine("🔍 App Log (Filtered):");
            foreach (var line in File.ReadLines(_logFile).Where(l => l.Contains("Applying")))
                _output.WriteLine(line);
        }

        Assert.Equal("15", speed);
        Assert.Equal("2", delay);
    }

    private static Process StartCmdProcess()
    {
        return Process.Start(new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = "/c ping 127.0.0.1 -n 3 >nul",
            CreateNoWindow = true,
            UseShellExecute = false
        })!;
    }

    private static bool IsAdministrator()
    {
        using var id = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(id);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }
}