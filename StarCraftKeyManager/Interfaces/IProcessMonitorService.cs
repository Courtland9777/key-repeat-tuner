namespace StarCraftKeyManager.Interfaces;

public interface IProcessMonitorService
{
    Task StartAsync(CancellationToken cancellationToken);
    Task StopAsync(CancellationToken cancellationToken);
}