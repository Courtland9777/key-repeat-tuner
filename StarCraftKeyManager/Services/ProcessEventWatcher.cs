using System.Management;
using MediatR;
using StarCraftKeyManager.Configuration.ValueObjects;
using StarCraftKeyManager.Events;
using StarCraftKeyManager.Interfaces;
using StarCraftKeyManager.SystemAdapters.Interfaces;
using StarCraftKeyManager.SystemAdapters.Wrappers;

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
        var sanitized = new ProcessName(processName).Value;

        if (string.Equals(_processName, sanitized, StringComparison.OrdinalIgnoreCase))
            return;

        Log.WatcherReconfigured(_logger, _processName, sanitized, null);

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
                $"SELECT * FROM Win32_ProcessStartTrace WHERE ProcessName = '{new ProcessName(_processName).WithExe()}'";
            var stopQuery =
                $"SELECT * FROM Win32_ProcessStopTrace WHERE ProcessName = '{new ProcessName(_processName).WithExe()}'";

            _startWatcher = _watcherFactory.Create(startQuery);
            _stopWatcher = _watcherFactory.Create(stopQuery);

            _startWatcher.EventArrived += (_, e) => OnStartEventArrived(e);
            _stopWatcher.EventArrived += (_, e) => OnStopEventArrived(e);

            _startWatcher.Start();
            _stopWatcher.Start();

            Log.WatchersStarted(_logger, null);
        }
        catch (Exception ex)
        {
            Log.StartFailure(_logger, ex);
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

            Log.WatchersStopped(_logger, null);
        }
        catch (Exception ex)
        {
            Log.StopFailure(_logger, ex);
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

        Log.StartEvent(_logger, pid, null);
        _ = _mediator.Publish(new ProcessStarted(pid, $"{_processName}.exe"));
    }

    internal void OnStopEventArrived(EventArrivedEventArgs e)
    {
        var args = _adapterFactory(e);
        var pid = args.GetProcessId();

        Log.StopEvent(_logger, pid, null);
        _ = _mediator.Publish(new ProcessStopped(pid, $"{_processName}.exe"));
    }

    private static class Log
    {
        public static readonly Action<ILogger, int, Exception?> StartEvent =
            LoggerMessage.Define<int>(
                LogLevel.Information,
                new EventId(2001, nameof(StartEvent)),
                "WMI Start Event: PID {Pid}");

        public static readonly Action<ILogger, int, Exception?> StopEvent =
            LoggerMessage.Define<int>(
                LogLevel.Information,
                new EventId(2002, nameof(StopEvent)),
                "WMI Stop Event: PID {Pid}");

        public static readonly Action<ILogger, string, string, Exception?> WatcherReconfigured =
            LoggerMessage.Define<string, string>(
                LogLevel.Information,
                new EventId(2003, nameof(WatcherReconfigured)),
                "Reconfiguring WMI process watcher for {OldName} → {NewName}");

        public static readonly Action<ILogger, Exception?> StartFailure =
            LoggerMessage.Define(
                LogLevel.Error,
                new EventId(2004, nameof(StartFailure)),
                "Failed to start WMI process watchers for process.");

        public static readonly Action<ILogger, Exception?> StopFailure =
            LoggerMessage.Define(
                LogLevel.Warning,
                new EventId(2005, nameof(StopFailure)),
                "Failed to stop/reconfigure watchers.");

        public static readonly Action<ILogger, Exception?> WatchersStarted =
            LoggerMessage.Define(
                LogLevel.Information,
                new EventId(2006, nameof(WatchersStarted)),
                "WMI process watchers started.");

        public static readonly Action<ILogger, Exception?> WatchersStopped =
            LoggerMessage.Define(
                LogLevel.Information,
                new EventId(2007, nameof(WatchersStopped)),
                "WMI process watchers stopped.");
    }
}