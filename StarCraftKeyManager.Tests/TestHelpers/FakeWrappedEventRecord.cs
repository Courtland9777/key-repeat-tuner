using StarCraftKeyManager.Adapters;

namespace StarCraftKeyManager.Tests.TestHelpers;

public sealed class FakeWrappedEventRecord : IWrappedEventRecord
{
    public FakeWrappedEventRecord(int id, string processName)
    {
        Id = id;
        Properties = new List<object?> { null, 1234, processName };
    }

    public FakeWrappedEventRecord(int id, object invalidProcessId, string processName)
    {
        Id = id;
        Properties = new List<object?> { null, invalidProcessId, processName };
    }

    public int Id { get; }
    public IReadOnlyList<object?> Properties { get; }
}