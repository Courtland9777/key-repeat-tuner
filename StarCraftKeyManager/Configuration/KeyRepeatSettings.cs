namespace StarCraftKeyManager.Configuration;

public class KeyRepeatSettings
{
    public required KeyRepeatState Default { get; init; }

    public required KeyRepeatState FastMode { get; init; }
}