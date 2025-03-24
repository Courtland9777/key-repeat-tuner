using System.Diagnostics;
using StarCraftKeyManager.Adapters;

namespace StarCraftKeyManager.Wrappers;

internal sealed class ProcessProvider : IProcessProvider
{
    public IEnumerable<int> GetProcessIdsByName(string name)
    {
        var trimmed = name.Replace(".exe", "", StringComparison.OrdinalIgnoreCase);
        return Process.GetProcessesByName(trimmed).Select(p => p.Id);
    }
}