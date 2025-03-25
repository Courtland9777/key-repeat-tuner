namespace StarCraftKeyManager.Adapters;

public interface IWrappedEventRecord
{
    int Id { get; }
    IReadOnlyList<object?> Properties { get; }
}