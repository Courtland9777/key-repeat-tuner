using System.Diagnostics;
using StarCraftKeyManager.SystemAdapters.Interfaces;

namespace StarCraftKeyManager.SystemAdapters.Wrappers;

internal sealed class ProcessProvider : IProcessProvider
{
    public IEnumerable<int> GetProcessIdsByName(string name)
    {
        var trimmed = name.Replace(".exe", "", StringComparison.OrdinalIgnoreCase);
        return Process.GetProcessesByName(trimmed).Select(p => p.Id);
    }
}