namespace StarCraftKeyManager.SystemAdapters.Interfaces;

public interface IProcessProvider
{
    IEnumerable<int> GetProcessIdsByName(string name);
}