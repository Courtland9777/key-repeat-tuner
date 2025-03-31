using System.Diagnostics;
using StarCraftKeyManager.SystemAdapters.Interfaces;
using StarCraftKeyManager.Utilities;

namespace StarCraftKeyManager.SystemAdapters.Wrappers;

internal sealed class ProcessProvider : IProcessProvider
{
    public IEnumerable<int> GetProcessIdsByName(string name)
    {
        var trimmed = ProcessNameSanitizer.Normalize(name);
        return Process.GetProcessesByName(trimmed).Select(p => p.Id);
    }
}