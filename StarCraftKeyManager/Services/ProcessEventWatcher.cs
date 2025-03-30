using System.Management;
using Microsoft.Extensions.Options;
using StarCraftKeyManager.Configuration;
using StarCraftKeyManager.Events;
using StarCraftKeyManager.Interfaces;

namespace StarCraftKeyManager.Services;

internal sealed class ProcessEventWatcher : IProcessEventWatcher
{
    private readonly ILogger<ProcessEventWatcher> _logger;
    private readonly IOptionsMonitor<AppSettings> _optionsMonitor;
    private string _processName = string.Empty;
    private ManagementEventWatcher? _startWatcher;
    private ManagementEventWatcher? _stopWatcher;

    public ProcessEventWatcher(
        ILogger<ProcessEventWatcher> logger,
        IOptionsMonitor<AppSettings> optionsMonitor)
    {
        _logger = logger;
        _optionsMonitor = optionsMonitor;
    }

    public event EventHandler<ProcessEventArgs>? ProcessEventOccurred;

    public void Configure(string processName)
    {
        _processName = processName.Replace(".exe", "", StringComparison.OrdinalIgnoreCase);
        _logger.LogInformation("Configured WMI process watcher for {ProcessName}", _processName);
    }

    public void Start()
    {
        if (_startWatcher != null || _stopWatcher != null) return;

        var startQuery = $"SELECT * FROM Win32_ProcessStartTrace WHERE ProcessName = '{_processName}.exe'";
        var stopQuery = $"SELECT * FROM Win32_ProcessStopTrace WHERE ProcessName = '{_processName}.exe'";

        _startWatcher = new ManagementEventWatcher(new WqlEventQuery(startQuery));
        _stopWatcher = new ManagementEventWatcher(new WqlEventQuery(stopQuery));

        _startWatcher.EventArrived += (s, e) =>
        {
            var pid = Convert.ToInt32(e.NewEvent.Properties["ProcessID"].Value);
            ProcessEventOccurred?.Invoke(this, new ProcessEventArgs(4688, pid, $"{_processName}.exe"));
        };

        _stopWatcher.EventArrived += (s, e) =>
        {
            var pid = Convert.ToInt32(e.NewEvent.Properties["ProcessID"].Value);
            ProcessEventOccurred?.Invoke(this, new ProcessEventArgs(4689, pid, $"{_processName}.exe"));
        };

        _startWatcher.Start();
        _stopWatcher.Start();

        _logger.LogInformation("WMI process watchers started.");
    }

    public void Stop()
    {
        _startWatcher?.Stop();
        _startWatcher?.Dispose();
        _startWatcher = null;

        _stopWatcher?.Stop();
        _stopWatcher?.Dispose();
        _stopWatcher = null;

        _logger.LogInformation("WMI process watchers stopped.");
    }

    public void Dispose()
    {
        Stop();
    }
}