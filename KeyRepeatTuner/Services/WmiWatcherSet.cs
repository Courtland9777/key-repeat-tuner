using System.Management;
using KeyRepeatTuner.SystemAdapters.Interfaces;

namespace KeyRepeatTuner.Services;

/// <summary>
///     Manages a matched pair of WMI event watchers for a specific process executable.
///     Handles Start and Stop trace events.
/// </summary>
internal sealed class WmiWatcherSet : IDisposable
{
    private readonly Action<int> _onStart;
    private readonly Action<int> _onStop;
    private readonly IManagementEventWatcher _startWatcher;
    private readonly IManagementEventWatcher _stopWatcher;

    public WmiWatcherSet(
        string exeName,
        IManagementEventWatcherFactory factory,
        Action<int> onStart,
        Action<int> onStop)
    {
        _onStart = onStart;
        _onStop = onStop;

        _startWatcher = factory.Create($"SELECT * FROM Win32_ProcessStartTrace WHERE ProcessName = '{exeName}'");
        _stopWatcher = factory.Create($"SELECT * FROM Win32_ProcessStopTrace WHERE ProcessName = '{exeName}'");

        _startWatcher.EventArrived += (_, e) => HandleStart(e);
        _stopWatcher.EventArrived += (_, e) => HandleStop(e);
    }

    public void Dispose()
    {
        Stop();
        _startWatcher.Dispose();
        _stopWatcher.Dispose();
    }

    public void Start()
    {
        _startWatcher.Start();
        _stopWatcher.Start();
    }

    public void Stop()
    {
        _startWatcher.Stop();
        _stopWatcher.Stop();
    }

    private void HandleStart(EventArrivedEventArgs e)
    {
        var pid = GetProcessId(e);
        _onStart(pid);
    }

    private void HandleStop(EventArrivedEventArgs e)
    {
        var pid = GetProcessId(e);
        _onStop(pid);
    }

    private static int GetProcessId(EventArrivedEventArgs e)
    {
        return Convert.ToInt32(e.NewEvent.Properties["ProcessID"].Value);
    }
}