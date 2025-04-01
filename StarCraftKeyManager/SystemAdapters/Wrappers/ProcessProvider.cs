using System.Diagnostics;
using StarCraftKeyManager.Configuration.ValueObjects;
using StarCraftKeyManager.SystemAdapters.Interfaces;

namespace StarCraftKeyManager.SystemAdapters.Wrappers;

internal sealed class ProcessProvider : IProcessProvider
{
    public IEnumerable<int> GetProcessIdsByName(string name)
    {
        var trimmed = new ProcessName(name).Value;
        return Process.GetProcessesByName(trimmed).Select(p => p.Id);
    }
}