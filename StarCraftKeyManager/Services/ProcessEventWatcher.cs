using System.Management;
using MediatR;
using StarCraftKeyManager.Configuration;
using StarCraftKeyManager.Configuration.ValueObjects;
using StarCraftKeyManager.Events;
using StarCraftKeyManager.Interfaces;
using StarCraftKeyManager.SystemAdapters.Interfaces;
using StarCraftKeyManager.SystemAdapters.Wrappers;

namespace StarCraftKeyManager.Services;

public sealed class ProcessEventWatcher : IProcessEventWatcher, IProcessNamesChangeHandler
{
    private readonly Func<EventArrivedEventArgs, IEventArrivedEventArgs> _adapterFactory;
    private readonly ILogger<ProcessEventWatcher> _logger;
    private readonly IMediator _mediator;
    private readonly IManagementEventWatcherFactory _watcherFactory;

    private readonly Dictionary<string, (IManagementEventWatcher start, IManagementEventWatcher stop)> _watchers =
        new(StringComparer.OrdinalIgnoreCase);

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
        Configure([processName]);
    }

    public void Start()
    {
        foreach (var (name, (start, stop)) in _watchers)
            try
            {
                start.Start();
                stop.Start();
            }
            catch (Exception ex)
            {
                Log.StartFailure(_logger, ex);
            }

        Log.WatchersStarted(_logger, null);
    }

    public void Stop()
    {
        foreach (var name in _watchers.Keys.ToList())
            StopWatcher(name);

        Log.WatchersStopped(_logger, null);
    }

    public void Dispose()
    {
        Stop();
    }

    public void OnProcessNamesChanged(List<string> added, List<string> removed)
    {
        foreach (var name in removed)
            StopWatcher(name);

        foreach (var name in added)
            StartWatcher(name);
    }

    public void OnSettingsChanged(AppSettings newSettings)
    {
        var newNames = newSettings.ProcessNames
            .Select(p => p.Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var currentNames = _watchers.Keys.ToList();

        var added = newNames.Except(currentNames, StringComparer.OrdinalIgnoreCase).ToList();
        var removed = currentNames.Except(newNames, StringComparer.OrdinalIgnoreCase).ToList();

        if (added.Count == 0 && removed.Count == 0)
        {
            _logger.LogDebug("Process watcher config unchanged. No updates required.");
            return;
        }

        _logger.LogInformation("Process watcher config changed. Added: {Added}, Removed: {Removed}", added, removed);

        OnProcessNamesChanged(added, removed);
    }


    private void Configure(List<string> processNames)
    {
        OnProcessNamesChanged(
            [.. processNames.Except(_watchers.Keys, StringComparer.OrdinalIgnoreCase)],
            [.. _watchers.Keys.Except(processNames, StringComparer.OrdinalIgnoreCase)]
        );
    }

    private void StartWatcher(string name)
    {
        var processName = new ProcessName(name);
        var exeName = processName.WithExe();

        try
        {
            var startQuery = $"SELECT * FROM Win32_ProcessStartTrace WHERE ProcessName = '{exeName}'";
            var stopQuery = $"SELECT * FROM Win32_ProcessStopTrace WHERE ProcessName = '{exeName}'";

            var startWatcher = _watcherFactory.Create(startQuery);
            var stopWatcher = _watcherFactory.Create(stopQuery);

            startWatcher.EventArrived += (_, e) => OnStartEventArrived(e, exeName);
            stopWatcher.EventArrived += (_, e) => OnStopEventArrived(e, exeName);

            startWatcher.Start();
            stopWatcher.Start();

            _watchers[processName.Value] = (startWatcher, stopWatcher);
            Log.WatcherReconfigured(_logger, "<none>", processName.Value, null);
        }
        catch (Exception ex)
        {
            Log.StartFailure(_logger, ex);
        }
    }

    private void StopWatcher(string name)
    {
        if (!_watchers.TryGetValue(name, out var watchers))
            return;

        try
        {
            watchers.start.Stop();
            watchers.stop.Stop();
            watchers.start.Dispose();
            watchers.stop.Dispose();
        }
        catch (Exception ex)
        {
            Log.StopFailure(_logger, ex);
        }

        _watchers.Remove(name);
    }

    internal void OnStartEventArrived(EventArrivedEventArgs e, string processName)
    {
        var pid = _adapterFactory(e).GetProcessId();
        Log.StartEvent(_logger, pid, null);
        _ = _mediator.Publish(new ProcessStarted(pid, processName));
    }

    internal void OnStopEventArrived(EventArrivedEventArgs e, string processName)
    {
        var pid = _adapterFactory(e).GetProcessId();
        Log.StopEvent(_logger, pid, null);
        _ = _mediator.Publish(new ProcessStopped(pid, processName));
    }

    public bool IsHealthy()
    {
        return _watchers.All(pair => pair.Value is { start: not null, stop: not null });
    }


    private static class Log
    {
        public static readonly Action<ILogger, int, Exception?> StartEvent =
            LoggerMessage.Define<int>(LogLevel.Information, new EventId(2001, nameof(StartEvent)),
                "WMI Start Event: PID {Pid}");

        public static readonly Action<ILogger, int, Exception?> StopEvent =
            LoggerMessage.Define<int>(LogLevel.Information, new EventId(2002, nameof(StopEvent)),
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