using System.Diagnostics;

namespace KeyRepeatTuner.SystemTests.TestUtilities.Helpers;

public sealed class KeyRepeatAppRunner : IDisposable
{
    private readonly string _exePath;
    private readonly string? _logPath;
    private Process? _appProcess;

    public KeyRepeatAppRunner(string? logPath = null)
    {
        _exePath = GetAppPath();
        _logPath = logPath;
    }

    public void Dispose()
    {
        if (_appProcess is { HasExited: false })
            _appProcess.Kill(true);

        _appProcess?.WaitForExit(5000);

        if (_logPath is not null && File.Exists(_logPath))
        {
            var logs = File.ReadAllText(_logPath);
            Console.WriteLine("Captured App Log Output:");
            Console.WriteLine(logs);
        }

        GC.SuppressFinalize(this);
    }

    public void Start(IEnumerable<KeyValuePair<string, string>>? envOverrides = null)
    {
        var startInfo = new ProcessStartInfo(_exePath)
        {
            UseShellExecute = false,
            WorkingDirectory = Path.GetDirectoryName(_exePath)!,
            RedirectStandardOutput = false,
            RedirectStandardError = false
        };

        if (envOverrides is not null)
            foreach (var kv in envOverrides)
            {
                var envKey = kv.Key.Replace(":", "__");
                startInfo.Environment[envKey] = kv.Value;
            }

        _appProcess = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start app.");
    }

    private static string GetAppPath()
    {
        var custom = Environment.GetEnvironmentVariable("KR_APP_PATH");
        if (!string.IsNullOrWhiteSpace(custom) && File.Exists(custom)) return custom;

        const string fallback =
            @"C:\Users\Court\source\repos\KeyRepeatTuner\KeyRepeatTuner\bin\Debug\net8.0-windows\win-x64\KeyRepeatTuner.exe";
        if (!File.Exists(fallback))
            throw new FileNotFoundException("KeyRepeatTuner.exe not found at expected location", fallback);

        return fallback;
    }
}