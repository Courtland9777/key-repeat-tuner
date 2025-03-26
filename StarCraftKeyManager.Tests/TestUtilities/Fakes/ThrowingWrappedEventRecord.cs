using StarCraftKeyManager.Adapters;

namespace StarCraftKeyManager.Tests.TestUtilities.Fakes;

public sealed class ThrowingWrappedEventRecord : IWrappedEventRecord
{
    public int Id => 4688;

    public IReadOnlyList<object?> Properties => throw new InvalidOperationException("Simulated failure");
}