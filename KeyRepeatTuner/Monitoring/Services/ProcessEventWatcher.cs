using System.Management;
using KeyRepeatTuner.Configuration;
using KeyRepeatTuner.Configuration.ValueObjects;
using KeyRepeatTuner.Monitoring.Interfaces;
using KeyRepeatTuner.SystemAdapters.Interfaces;
using KeyRepeatTuner.SystemAdapters.WMI;

namespace KeyRepeatTuner.Monitoring.Services;

internal sealed class ProcessEventWatcher : IProcessEventWatcher
{
    private readonly Func<EventArrivedEventArgs, IEventArrivedEventArgs> _adapterFactory;
    private readonly ILogger<ProcessEventWatcher> _logger;
    private readonly IProcessEventRouter _router;
    private readonly IManagementEventWatcherFactory _watcherFactory;

    private readonly Dictionary<string, WmiWatcherSet> _watchers = new(StringComparer.OrdinalIgnoreCase);

    public ProcessEventWatcher(
        ILogger<ProcessEventWatcher> logger,
        IManagementEventWatcherFactory watcherFactory,
        IProcessEventRouter router,
        Func<EventArrivedEventArgs, IEventArrivedEventArgs>? adapterFactory = null)
    {
        _logger = logger;
        _watcherFactory = watcherFactory;
        _router = router;
        _adapterFactory = adapterFactory ?? (e => new EventArrivedEventArgsAdapter(e));
    }

    public void Start()
    {
        foreach (var watcher in _watchers.Values)
            try
            {
                watcher.Start();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start WMI watchers for process.");
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

    private void Configure(List<string> processNames)
    {
        var added = processNames.Except(_watchers.Keys, StringComparer.OrdinalIgnoreCase).ToList();
        var removed = _watchers.Keys.Except(processNames, StringComparer.OrdinalIgnoreCase).ToList();

        OnProcessNamesChanged(added, removed);
    }

    private void OnProcessNamesChanged(List<string> added, List<string> removed)
    {
        foreach (var name in removed) StopWatcher(name);

        foreach (var name in added) StartWatcher(name);
    }

    private void StartWatcher(string name)
    {
        var processName = new ProcessName(name);
        var exeName = processName.WithExe();

        try
        {
            var watcherSet = new WmiWatcherSet(
                exeName,
                _watcherFactory,
                pid => OnStartEventArrived(pid, exeName),
                pid => OnStopEventArrived(pid, exeName));

            watcherSet.Start();
            _watchers[processName.Value] = watcherSet;

            _logger.LogInformation("WMI process watcher is now watching → {ProcessName}", processName.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start WMI process watchers for process.");
        }
    }

    private void StopWatcher(string name)
    {
        if (!_watchers.TryGetValue(name, out var watcherSet))
            return;

        try
        {
            watcherSet.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to stop/reconfigure watchers.");
        }

        _watchers.Remove(name);
    }

    internal void OnStartEventArrived(int pid, string processName)
    {
        _logger.LogInformation("WMI Start Event: PID {Pid}", pid);
        _router.OnProcessStarted(pid, processName);
    }

    internal void OnStopEventArrived(int pid, string processName)
    {
        _logger.LogInformation("WMI Stop Event: PID {Pid}", pid);
        _router.OnProcessStopped(pid, processName);
    }
}