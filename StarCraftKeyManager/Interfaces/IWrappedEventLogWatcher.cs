using System.Diagnostics.Eventing.Reader;

namespace StarCraftKeyManager.Interfaces;

public interface IWrappedEventLogWatcher : IDisposable
{
    bool Enabled { get; set; }
    event EventHandler<EventRecordWrittenEventArgs> EventRecordWritten;
}