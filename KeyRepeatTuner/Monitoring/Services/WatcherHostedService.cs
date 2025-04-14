using KeyRepeatTuner.Monitoring.Interfaces;

namespace KeyRepeatTuner.Monitoring.Services;

public sealed class WatcherHostedService : IHostedService
{
    private readonly IHostApplicationLifetime _lifetime;
    private readonly ILogger<WatcherHostedService> _logger;
    private readonly StartupWatcherTrigger _trigger;
    private readonly IProcessEventWatcher _watcher;

    public WatcherHostedService(
        ILogger<WatcherHostedService> logger,
        StartupWatcherTrigger trigger,
        IProcessEventWatcher watcher,
        IHostApplicationLifetime lifetime)
    {
        _logger = logger;
        _trigger = trigger;
        _watcher = watcher;
        _lifetime = lifetime;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("WatcherHostedService starting.");

        try
        {
            _trigger.Trigger();
            _lifetime.ApplicationStopping.Register(OnShutdown);
            _logger.LogInformation("WatcherHostedService started successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start WatcherHostedService.");
            throw;
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("WatcherHostedService stopping.");

        try
        {
            _watcher.Dispose();
            _logger.LogInformation("WMI watchers disposed.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error during shutdown cleanup.");
        }

        return Task.CompletedTask;
    }

    private void OnShutdown()
    {
        _logger.LogInformation("Application shutdown triggered. Cleaning up WatcherHostedService.");
        _watcher.Dispose(); // double safety
    }
}