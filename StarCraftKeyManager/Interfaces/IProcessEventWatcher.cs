using StarCraftKeyManager.Models;

namespace StarCraftKeyManager.Interfaces;

public interface IProcessEventWatcher : IDisposable
{
    void Configure(string processName);
    void Start();
    void Stop();
    event EventHandler<ProcessEventArgs>? ProcessEventOccurred;
}