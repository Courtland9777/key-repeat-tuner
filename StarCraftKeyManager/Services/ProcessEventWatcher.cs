using System.Management;
using Microsoft.Extensions.Options;
using StarCraftKeyManager.Configuration;
using StarCraftKeyManager.Events;
using StarCraftKeyManager.Interfaces;
using StarCraftKeyManager.SystemAdapters.Interfaces;

namespace StarCraftKeyManager.Services;

public class ProcessEventWatcher : IProcessEventWatcher
{
    private readonly ILogger<ProcessEventWatcher> _logger;
    private readonly IOptionsMonitor<AppSettings> _optionsMonitor;
    private readonly IManagementEventWatcherFactory _watcherFactory;
    private string _processName = string.Empty;
    private IManagementEventWatcher? _startWatcher;
    private IManagementEventWatcher? _stopWatcher;

    public ProcessEventWatcher(
        ILogger<ProcessEventWatcher> logger,
        IOptionsMonitor<AppSettings> optionsMonitor,
        IManagementEventWatcherFactory watcherFactory)
    {
        _logger = logger;
        _optionsMonitor = optionsMonitor;
        _watcherFactory = watcherFactory;
    }

    public event EventHandler<ProcessEventArgs>? ProcessEventOccurred;

    public void Configure(string processName)
    {
        var sanitized = processName.Replace(".exe", string.Empty, StringComparison.OrdinalIgnoreCase);

        if (string.Equals(_processName, sanitized, StringComparison.OrdinalIgnoreCase))
            return;

        _logger.LogInformation("Reconfiguring WMI process watcher for {OldName} → {NewName}", _processName, sanitized);

        Stop();
        _processName = sanitized;
        Start();
    }

    public virtual void Stop()
    {
        _startWatcher?.Stop();
        _startWatcher?.Dispose();
        _startWatcher = null;

        _stopWatcher?.Stop();
        _stopWatcher?.Dispose();
        _stopWatcher = null;

        _logger.LogInformation("WMI process watchers stopped.");
    }

    public void Start()
    {
        if (_startWatcher != null || _stopWatcher != null) return;

        var startQuery = $"SELECT * FROM Win32_ProcessStartTrace WHERE ProcessName = '{_processName}.exe'";
        var stopQuery = $"SELECT * FROM Win32_ProcessStopTrace WHERE ProcessName = '{_processName}.exe'";

        _startWatcher = _watcherFactory.Create(startQuery);
        _stopWatcher = _watcherFactory.Create(stopQuery);

        _startWatcher.EventArrived += (s, e) => OnStartEventArrived(e);
        _stopWatcher.EventArrived += (s, e) => OnStopEventArrived(e);

        _startWatcher.Start();
        _stopWatcher.Start();

        _logger.LogInformation("WMI process watchers started.");
    }

    public void Dispose()
    {
        Stop();
        GC.SuppressFinalize(this);
    }

    protected virtual void OnStartEventArrived(EventArrivedEventArgs e)
    {
        HandleStartEvent(e);
    }

    protected virtual void OnStopEventArrived(EventArrivedEventArgs e)
    {
        HandleStopEvent(e);
    }

    private void HandleStartEvent(EventArrivedEventArgs e)
    {
        var pid = ExtractProcessId(e);
        _logger.LogInformation("WMI Start Event: PID {Pid}", pid);
        ProcessEventOccurred?.Invoke(this, new ProcessEventArgs(4688, pid, $"{_processName}.exe"));
    }

    private void HandleStopEvent(EventArrivedEventArgs e)
    {
        var pid = ExtractProcessId(e);
        _logger.LogInformation("WMI Stop Event: PID {Pid}", pid);
        ProcessEventOccurred?.Invoke(this, new ProcessEventArgs(4689, pid, $"{_processName}.exe"));
    }

    protected virtual int ExtractProcessId(EventArrivedEventArgs e)
    {
        return Convert.ToInt32(e.NewEvent.Properties["ProcessID"].Value);
    }
}