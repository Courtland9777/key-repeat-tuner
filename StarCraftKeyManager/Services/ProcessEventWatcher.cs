using System.Management;
using StarCraftKeyManager.Events;
using StarCraftKeyManager.Interfaces;
using StarCraftKeyManager.SystemAdapters.Interfaces;
using StarCraftKeyManager.SystemAdapters.Wrappers;
using StarCraftKeyManager.Utilities;

namespace StarCraftKeyManager.Services;

public sealed class ProcessEventWatcher : IProcessEventWatcher
{
    private readonly ILogger<ProcessEventWatcher> _logger;
    private readonly IManagementEventWatcherFactory _watcherFactory;
    private string _processName = string.Empty;
    private IManagementEventWatcher? _startWatcher;
    private IManagementEventWatcher? _stopWatcher;

    public ProcessEventWatcher(
        ILogger<ProcessEventWatcher> logger,
        IManagementEventWatcherFactory watcherFactory)
    {
        _logger = logger;
        _watcherFactory = watcherFactory;
    }

    public event EventHandler<ProcessEventArgs>? ProcessEventOccurred;

    public void Configure(string processName)
    {
        var sanitized = ProcessNameSanitizer.Normalize(processName);

        if (string.Equals(_processName, sanitized, StringComparison.OrdinalIgnoreCase))
            return;

        _logger.LogInformation("Reconfiguring WMI process watcher for {OldName} → {NewName}", _processName, sanitized);

        Stop();
        _processName = sanitized;
        Start();
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

    public void Start()
    {
        if (_startWatcher != null || _stopWatcher != null) return;

        var exeName = $"{_processName}.exe";
        var startQuery = $"SELECT * FROM Win32_ProcessStartTrace WHERE ProcessName = '{exeName}'";
        var stopQuery = $"SELECT * FROM Win32_ProcessStopTrace WHERE ProcessName = '{exeName}'";


        _startWatcher = _watcherFactory.Create(startQuery);
        _stopWatcher = _watcherFactory.Create(stopQuery);

        _startWatcher.EventArrived += (_, e) => OnStartEventArrived(e);
        _stopWatcher.EventArrived += (_, e) => OnStopEventArrived(e);

        _startWatcher.Start();
        _stopWatcher.Start();

        _logger.LogInformation("WMI process watchers started.");
    }

    public void Dispose()
    {
        Stop();
    }

    private void OnStartEventArrived(EventArrivedEventArgs e)
    {
        HandleStartEvent(new EventArrivedEventArgsAdapter(e));
    }

    private void OnStopEventArrived(EventArrivedEventArgs e)
    {
        HandleStopEvent(new EventArrivedEventArgsAdapter(e));
    }

    private void HandleStartEvent(IEventArrivedEventArgs args)
    {
        var pid = args.GetProcessId();
        _logger.LogInformation("WMI Start Event: PID {Pid}", pid);
        ProcessEventOccurred?.Invoke(this, new ProcessEventArgs(4688, pid, $"{_processName}.exe"));
    }

    private void HandleStopEvent(IEventArrivedEventArgs args)
    {
        var pid = args.GetProcessId();
        _logger.LogInformation("WMI Stop Event: PID {Pid}", pid);
        ProcessEventOccurred?.Invoke(this, new ProcessEventArgs(4689, pid, $"{_processName}.exe"));
    }
}