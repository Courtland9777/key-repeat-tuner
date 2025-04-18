using KeyRepeatTuner.Core.Interfaces;
using KeyRepeatTuner.Monitoring.Interfaces;

namespace KeyRepeatTuner.Monitoring.Services;

public sealed class WatcherHostedService : IHostedService
{
    private readonly IHostApplicationLifetime _lifetime;
    private readonly ILogger<WatcherHostedService> _logger;
    private readonly IStartupWatcherTrigger _trigger;
    private readonly IProcessEventWatcher _watcher;

    public WatcherHostedService(
        ILogger<WatcherHostedService> logger,
        IStartupWatcherTrigger trigger,
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
            _logger.LogInformation("WatcherHostedService starting...");
            _trigger.Trigger();
            _logger.LogInformation("WMI watcher trigger initialized.");
            _lifetime.ApplicationStopping.Register(OnShutdown);
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
        _logger.LogInformation("Application shutdown triggered. Disposing WMI watchers...");
        _watcher.Dispose();
    }
}