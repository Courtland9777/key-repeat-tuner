using System.Diagnostics;
using KeyRepeatTuner.Configuration.ValueObjects;
using KeyRepeatTuner.SystemAdapters.Interfaces;

namespace KeyRepeatTuner.SystemAdapters.Wrappers;

internal sealed class ProcessProvider : IProcessProvider
{
    public IEnumerable<int> GetProcessIdsByName(string name)
    {
        var trimmed = new ProcessName(name).Value;
        return Process.GetProcessesByName(trimmed).Select(p => p.Id);
    }
}