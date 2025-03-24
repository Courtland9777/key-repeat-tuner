using System.Diagnostics.Eventing.Reader;
using StarCraftKeyManager.Interfaces;

namespace StarCraftKeyManager.Wrappers;

internal sealed class WrappedEventLogWatcher : IWrappedEventLogWatcher
{
    private readonly EventLogWatcher _watcher;

    public WrappedEventLogWatcher(EventLogWatcher watcher)
    {
        _watcher = watcher;
    }

    public bool Enabled
    {
        get => _watcher.Enabled;
        set => _watcher.Enabled = value;
    }

    public event EventHandler<EventRecordWrittenEventArgs>
        EventRecordWritten
        {
            add => _watcher.EventRecordWritten += value;
            remove => _watcher.EventRecordWritten -= value;
        }

    public void Dispose()
    {
        _watcher.Dispose();
    }
}