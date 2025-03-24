namespace StarCraftKeyManager.Adapters;

public interface IProcessProvider
{
    IEnumerable<int> GetProcessIdsByName(string name);
}