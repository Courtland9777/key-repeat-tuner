using System.Diagnostics.Eventing.Reader;
using StarCraftKeyManager.Adapters;

namespace StarCraftKeyManager.Wrappers;

internal sealed class WrappedEventRecord : IWrappedEventRecord
{
    private readonly EventRecord _record;

    public WrappedEventRecord(EventRecord record)
    {
        _record = record;
    }

    public int Id => _record.Id;
    public IReadOnlyList<object?> Properties => [.. _record.Properties.Select(p => p.Value)];
}