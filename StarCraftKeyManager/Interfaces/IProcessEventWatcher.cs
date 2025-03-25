namespace StarCraftKeyManager.Interfaces;

public interface IProcessEventWatcher : IDisposable
{
    event EventHandler<ProcessEventArgs>? ProcessEventOccurred;
    void Configure(string processName);
    void Start();
    void Stop();
}