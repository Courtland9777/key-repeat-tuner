using System.Management;
using MediatR;
using StarCraftKeyManager.Events;
using StarCraftKeyManager.Interfaces;
using StarCraftKeyManager.SystemAdapters.Interfaces;
using StarCraftKeyManager.SystemAdapters.Wrappers;
using StarCraftKeyManager.Utilities;

namespace StarCraftKeyManager.Services;

public sealed class ProcessEventWatcher : IProcessEventWatcher
{
    private readonly Func<EventArrivedEventArgs, IEventArrivedEventArgs> _adapterFactory;
    private readonly ILogger<ProcessEventWatcher> _logger;
    private readonly IMediator _mediator;
    private readonly IManagementEventWatcherFactory _watcherFactory;
    private string _processName = string.Empty;
    private IManagementEventWatcher? _startWatcher;
    private IManagementEventWatcher? _stopWatcher;

    public ProcessEventWatcher(
        ILogger<ProcessEventWatcher> logger,
        IManagementEventWatcherFactory watcherFactory,
        IMediator mediator,
        Func<EventArrivedEventArgs, IEventArrivedEventArgs>? adapterFactory = null)
    {
        _logger = logger;
        _watcherFactory = watcherFactory;
        _mediator = mediator;
        _adapterFactory = adapterFactory ?? (e => new EventArrivedEventArgsAdapter(e));
    }

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

    public void Start()
    {
        try
        {
            if (_startWatcher != null || _stopWatcher != null) return;

            var startQuery =
                $"SELECT * FROM Win32_ProcessStartTrace WHERE ProcessName = '{ProcessNameSanitizer.WithExe(_processName)}'";
            var stopQuery =
                $"SELECT * FROM Win32_ProcessStopTrace WHERE ProcessName = '{ProcessNameSanitizer.WithExe(_processName)}.exe'";

            _startWatcher = _watcherFactory.Create(startQuery);
            _stopWatcher = _watcherFactory.Create(stopQuery);

            _startWatcher.EventArrived += (_, e) => OnStartEventArrived(e);
            _stopWatcher.EventArrived += (_, e) => OnStopEventArrived(e);

            _startWatcher.Start();
            _stopWatcher.Start();

            _logger.LogInformation("WMI process watchers started.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start WMI process watchers for process: {ProcessName}", _processName);
            throw;
        }
    }

    public void Stop()
    {
        try
        {
            _startWatcher?.Stop();
            _startWatcher?.Dispose();
            _startWatcher = null;

            _stopWatcher?.Stop();
            _stopWatcher?.Dispose();
            _stopWatcher = null;

            _logger.LogInformation("WMI process watchers stopped.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to stop/reconfigure watchers.");
        }
    }

    public void Dispose()
    {
        Stop();
    }

    internal void OnStartEventArrived(EventArrivedEventArgs e)
    {
        var args = _adapterFactory(e);
        var pid = args.GetProcessId();
        _logger.LogInformation("WMI Start Event: PID {Pid}", pid);

        _ = _mediator.Publish(new ProcessStarted(pid, $"{_processName}.exe"));
    }

    internal void OnStopEventArrived(EventArrivedEventArgs e)
    {
        var args = _adapterFactory(e);
        var pid = args.GetProcessId();
        _logger.LogInformation("WMI Stop Event: PID {Pid}", pid);

        _ = _mediator.Publish(new ProcessStopped(pid, $"{_processName}.exe"));
    }
}