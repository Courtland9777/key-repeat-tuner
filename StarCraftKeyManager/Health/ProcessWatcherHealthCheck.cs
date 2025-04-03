using KeyRepeatTuner.Services;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace KeyRepeatTuner.Health;

/// <summary>
///     Health check to verify the WMI process event watchers are active.
/// </summary>
public class ProcessWatcherHealthCheck : IHealthCheck
{
    private readonly ProcessEventWatcher _watcher;

    public ProcessWatcherHealthCheck(ProcessEventWatcher watcher)
    {
        _watcher = watcher;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var status = _watcher.IsHealthy();

        return Task.FromResult(status
            ? HealthCheckResult.Healthy("Process watchers are active.")
            : HealthCheckResult.Unhealthy("One or more WMI watchers are inactive or disposed."));
    }
}