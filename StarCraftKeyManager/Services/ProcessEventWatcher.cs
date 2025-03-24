using System.Diagnostics.Eventing.Reader;
using Microsoft.Extensions.Options;
using StarCraftKeyManager.Interfaces;
using StarCraftKeyManager.Models;

namespace StarCraftKeyManager.Services;

internal sealed class ProcessEventWatcher : IProcessEventWatcher
{
    private readonly ILogger<ProcessEventWatcher> _logger;
    private readonly IOptionsMonitor<AppSettings> _optionsMonitor;
    private readonly IEventLogQueryBuilder _queryBuilder;
    private readonly IEventWatcherFactory _watcherFactory;
    private EventHandler<EventRecordWrittenEventArgs>? _eventHandler;
    private EventLogWatcher? _eventWatcher;
    private bool _isStarted;

    public ProcessEventWatcher(
        ILogger<ProcessEventWatcher> logger,
        IOptionsMonitor<AppSettings> optionsMonitor,
        IEventWatcherFactory watcherFactory,
        IEventLogQueryBuilder queryBuilder)
    {
        _logger = logger;
        _optionsMonitor = optionsMonitor;
        _watcherFactory = watcherFactory;
        _queryBuilder = queryBuilder;
    }

    public event EventHandler<ProcessEventArgs>? ProcessEventOccurred;

    public void Configure(string processName)
    {
        var query = _queryBuilder.BuildQuery();
        _eventWatcher = _watcherFactory.Create(query);
        _eventHandler = EventWatcherOnEventRecordWritten;
        _logger.LogInformation("Configured process watcher for {ProcessName}", processName);
    }


    public void Start()
    {
        if (_isStarted || _eventWatcher == null) return;

        _eventWatcher.EventRecordWritten += _eventHandler;
        _eventWatcher.Enabled = true;
        _isStarted = true;

        _logger.LogInformation("Process event watcher started.");
    }

    public void Stop()
    {
        if (!_isStarted || _eventWatcher == null) return;

        _eventWatcher.Enabled = false;
        _eventWatcher.EventRecordWritten -= _eventHandler;
        _eventWatcher.Dispose();
        _eventWatcher = null;
        _isStarted = false;

        _logger.LogInformation("Process event watcher stopped.");
    }

    public void Dispose()
    {
        Stop();
    }

    public void EventWatcherOnEventRecordWritten(object? sender, EventRecordWrittenEventArgs e)
    {
        if (e.EventRecord == null) return;

        try
        {
            var eventId = e.EventRecord.Id;
            var processId = ExtractProcessId(e.EventRecord);

            if (processId == null)
            {
                _logger.LogWarning("Failed to extract process ID from event record.");
                return;
            }

            _logger.LogInformation("Detected process event: EventId={EventId}, ProcessId={ProcessId}", eventId,
                processId);
            ProcessEventOccurred?.Invoke(this,
                new ProcessEventArgs(eventId, processId.Value,
                    _optionsMonitor.CurrentValue.ProcessMonitor.ProcessName));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing event record.");
        }
    }

    private int? ExtractProcessId(EventRecord eventRecord)
    {
        try
        {
            return (int?)eventRecord.Properties[1].Value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting process ID from event record.");
            return null;
        }
    }
}