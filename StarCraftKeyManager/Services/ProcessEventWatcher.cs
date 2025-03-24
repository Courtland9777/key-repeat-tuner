using System.Diagnostics.Eventing.Reader;
using Microsoft.Extensions.Options;
using StarCraftKeyManager.Adapters;
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

    private IWrappedEventLogWatcher? _eventWatcher;
    private bool _isRunning;

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
        if (_isRunning || _eventWatcher == null)
            return;

        _eventWatcher.EventRecordWritten += _eventHandler!;
        _eventWatcher.Enabled = true;
        _isRunning = true;

        _logger.LogInformation("Process event watcher started.");
    }

    public void Stop()
    {
        if (!_isRunning || _eventWatcher == null)
            return;

        _eventWatcher.Enabled = false;
        _eventWatcher.EventRecordWritten -= _eventHandler!;
        _eventWatcher.Dispose();

        _eventWatcher = null;
        _isRunning = false;

        _logger.LogInformation("Process event watcher stopped.");
    }

    public void Dispose()
    {
        Stop();
    }

    internal void EventWatcherOnEventRecordWritten(object? sender, EventRecordWrittenEventArgs e)
    {
        if (e.EventRecord == null)
        {
            _logger.LogWarning("Null EventRecord received from event log.");
            return;
        }

        try
        {
            var eventId = e.EventRecord.Id;
            var processId = ExtractProcessId(e.EventRecord);

            if (processId == null)
            {
                _logger.LogWarning("Missing required properties from event record.");
                return;
            }

            _logger.LogInformation("Detected process event: EventId={EventId}, ProcessId={ProcessId}", eventId,
                processId);

            ProcessEventOccurred?.Invoke(this, new ProcessEventArgs(
                eventId,
                processId.Value,
                _optionsMonitor.CurrentValue.ProcessMonitor.ProcessName
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing event record.");
        }
    }

    private int? ExtractProcessId(EventRecord record)
    {
        try
        {
            return (int?)record.Properties[1].Value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting process ID from event record.");
            return null;
        }
    }

    public void HandleEvent(IWrappedEventRecordWrittenEventArgs e)
    {
        if (e.EventRecord == null)
        {
            _logger.LogWarning("Null EventRecord received from event log.");
            return;
        }

        try
        {
            var eventId = e.EventRecord.Id;
            var processId = ExtractProcessId(e.EventRecord);

            if (processId == null)
            {
                _logger.LogWarning("Missing required properties from wrapped event record.");
                return;
            }

            _logger.LogInformation("Detected process event: EventId={EventId}, ProcessId={ProcessId}", eventId,
                processId);

            ProcessEventOccurred?.Invoke(this, new ProcessEventArgs(
                eventId,
                processId.Value,
                _optionsMonitor.CurrentValue.ProcessMonitor.ProcessName
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing wrapped event record.");
        }
    }

    private int? ExtractProcessId(IWrappedEventRecord record)
    {
        try
        {
            return record.Properties.Count > 1 ? Convert.ToInt32(record.Properties[1]) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting process ID from wrapped event record.");
            return null;
        }
    }
}