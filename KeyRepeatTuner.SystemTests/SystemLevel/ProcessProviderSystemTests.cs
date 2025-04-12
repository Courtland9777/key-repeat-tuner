using System.Diagnostics;
using System.Security.Principal;
using KeyRepeatTuner.SystemAdapters.Wrappers;
using Xunit;

namespace KeyRepeatTuner.SystemTests.SystemLevel;

public class ProcessProviderSystemTests : IDisposable
{
    private Process? _notepad;

    public ProcessProviderSystemTests()
    {
        Assert.True(IsAdministrator(), "System tests must be run as administrator.");
    }

    public void Dispose()
    {
        if (_notepad is { HasExited: false })
            _notepad.Kill(true);

        _notepad?.WaitForExit(3000);
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void ProcessProvider_ShouldReturnProcessId_WhenProcessIsRunning()
    {
        // Arrange
        _notepad = Process.Start("notepad.exe");
        Assert.NotNull(_notepad);

        var provider = new ProcessProvider();

        // Act
        var processIds = provider.GetProcessIdsByName("notepad").ToList();

        // Assert
        Assert.Contains(_notepad.Id, processIds);
    }


    private static bool IsAdministrator()
    {
        using var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }
}