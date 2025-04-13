using KeyRepeatTuner.Interfaces;

namespace KeyRepeatTuner.Services;

public class WatcherHostedService : BackgroundService
{
    private readonly ILogger<WatcherHostedService> _logger;
    private readonly StartupWatcherTrigger _trigger;
    private readonly IProcessEventWatcher _watcher;

    public WatcherHostedService(
        ILogger<WatcherHostedService> logger,
        StartupWatcherTrigger trigger,
        IProcessEventWatcher watcher)
    {
        _logger = logger;
        _trigger = trigger;
        _watcher = watcher;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting WatcherHostedService.");

        _trigger.Trigger();

        stoppingToken.Register(() =>
        {
            _logger.LogInformation("Cancellation requested. Disposing watcher.");
            _watcher.Dispose();
        });

        // Keep the service alive
        return Task.CompletedTask;
    }
}