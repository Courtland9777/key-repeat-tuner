using System.Management;
using KeyRepeatTuner.Configuration;
using KeyRepeatTuner.Configuration.ValueObjects;
using KeyRepeatTuner.Events;
using KeyRepeatTuner.Interfaces;
using KeyRepeatTuner.SystemAdapters.Interfaces;
using KeyRepeatTuner.SystemAdapters.Wrappers;
using MediatR;

namespace KeyRepeatTuner.Services;

public sealed class ProcessEventWatcher : IProcessEventWatcher
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
        foreach (var (_, (start, stop)) in _watchers)
            try
            {
                start.Start();
                stop.Start();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start WMI process watchers for process.");
            }

        _logger.LogInformation("WMI process watchers started.");
    }

    public void Stop()
    {
        foreach (var name in _watchers.Keys.ToList()) StopWatcher(name);

        _logger.LogInformation("WMI process watchers stopped.");
    }

    public void Dispose()
    {
        Stop();
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

    private void OnProcessNamesChanged(List<string> added, List<string> removed)
    {
        foreach (var name in removed)
            StopWatcher(name);

        foreach (var name in added)
            StartWatcher(name);
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

            _logger.LogInformation("WMI process watcher is now watching → {ProcessName}", processName.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start WMI process watchers for process.");
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
            _logger.LogWarning(ex, "Failed to stop/reconfigure watchers.");
        }

        _watchers.Remove(name);
    }

    internal void OnStartEventArrived(EventArrivedEventArgs e, string processName)
    {
        var pid = _adapterFactory(e).GetProcessId();
        _logger.LogInformation("WMI Start Event: PID {Pid}", pid);
        _ = _mediator.Publish(new ProcessStarted(pid, processName));
    }

    internal void OnStopEventArrived(EventArrivedEventArgs e, string processName)
    {
        var pid = _adapterFactory(e).GetProcessId();
        _logger.LogInformation("WMI Stop Event: PID {Pid}", pid);
        _ = _mediator.Publish(new ProcessStopped(pid, processName));
    }

    public bool IsHealthy()
    {
        return _watchers.All(pair => pair.Value is { start: not null, stop: not null });
    }
}