using StarCraftKeyManager.Adapters;

namespace StarCraftKeyManager.Tests.TestUtilities.Fakes;

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

    public FakeWrappedEventRecord(int id, IReadOnlyList<object?> customProperties)
    {
        Id = id;
        Properties = customProperties;
    }

    public int Id { get; }
    public IReadOnlyList<object?> Properties { get; }
}