using System.Diagnostics.Eventing.Reader;
using StarCraftKeyManager.Interfaces;
using StarCraftKeyManager.Models;

namespace StarCraftKeyManager.Services;

internal sealed class ProcessEventWatcher : IProcessEventWatcher, IDisposable
{
    private readonly ILogger<ProcessEventWatcher> _logger;
    private EventLogWatcher? _eventWatcher;
    private string _processName = string.Empty;

    public ProcessEventWatcher(ILogger<ProcessEventWatcher> logger)
    {
        _logger = logger;
    }

    public event EventHandler<ProcessEventArgs>? ProcessEventOccurred;

    public void Configure(string processName)
    {
        _processName = processName.Replace(".exe", string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    public void Start()
    {
        var query = $@"
        <QueryList>
            <Query Id='0' Path='Security'>
                <Select Path='Security'>
                    *[System[(EventID=4688 or EventID=4689)]]
                    and *[EventData[Data[@Name='NewProcessName'] and (contains(.,'{_processName}'))]]
                </Select>
            </Query>
        </QueryList>";

        var eventQuery = new EventLogQuery("Security", PathType.LogName, query);
        _eventWatcher = new EventLogWatcher(eventQuery);
        _eventWatcher.EventRecordWritten += EventWatcherOnEventRecordWritten;
        _eventWatcher.Enabled = true;

        _logger.LogInformation("Process event watcher started for '{ProcessName}'.", _processName);
    }

    public void Stop()
    {
        if (_eventWatcher != null)
        {
            _eventWatcher.EventRecordWritten -= EventWatcherOnEventRecordWritten;
            _eventWatcher.Dispose();
            _eventWatcher = null;
        }

        _logger.LogInformation("Process event watcher stopped for '{ProcessName}'.", _processName);
    }

    public void Dispose()
    {
        Stop();
    }

    private void EventWatcherOnEventRecordWritten(object? sender, EventRecordWrittenEventArgs e)
    {
        if (e.EventRecord == null || e.EventRecord.Properties.Count == 0)
            return;

        var eventId = e.EventRecord.Id;
        var processId = eventId == 4688
            ? e.EventRecord.Properties[4].Value as int?
            : e.EventRecord.Properties[3].Value as int?;

        var processPath = e.EventRecord.Properties.Count > 5
            ? e.EventRecord.Properties[5].Value as string ?? string.Empty
            : string.Empty;

        var detectedProcessName = Path.GetFileNameWithoutExtension(processPath);

        if (!string.Equals(detectedProcessName, _processName, StringComparison.OrdinalIgnoreCase))
            return;

        if (!processId.HasValue) return;
        ProcessEventOccurred?.Invoke(this, new ProcessEventArgs(eventId, processId.Value, detectedProcessName));
        _logger.LogDebug("Event handled: {EventId}, PID: {ProcessId}, ProcessName: '{ProcessName}'",
            eventId, processId, detectedProcessName);
    }
}