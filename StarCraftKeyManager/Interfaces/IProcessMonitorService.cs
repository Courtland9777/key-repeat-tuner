namespace StarCraftKeyManager.Interfaces;

public interface IProcessMonitorService
{
    public interface IProcessMonitorService
    {
        /// <summary>
        /// Starts monitoring the specified process asynchronously.
        /// </summary>
        Task StartMonitoringAsync(CancellationToken stoppingToken);

        /// <summary>
        /// Updates the key repeat settings based on the monitored process state.
        /// </summary>
        void ApplyKeyRepeatSettings();
    }
}