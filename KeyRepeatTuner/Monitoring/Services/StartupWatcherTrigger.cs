using KeyRepeatTuner.Configuration;
using KeyRepeatTuner.Core.Interfaces;
using KeyRepeatTuner.Monitoring.Interfaces;
using Microsoft.Extensions.Options;

namespace KeyRepeatTuner.Monitoring.Services;

internal class StartupWatcherTrigger : IStartupWatcherTrigger
{
    private static readonly TimeSpan DebounceWindow = TimeSpan.FromMilliseconds(500);
    private readonly IProcessEventWatcher _eventWatcher;
    private readonly ILogger<StartupWatcherTrigger> _logger;
    private readonly IOptionsMonitor<AppSettings> _optionsMonitor;
    private readonly IProcessEventRouter _router;
    private readonly IAppSettingsChangeHandler _settingsHandler;
    private readonly IKeyRepeatSettingsService _settingsService;

    private DateTime _lastUpdate = DateTime.MinValue;

    public StartupWatcherTrigger(
        IOptionsMonitor<AppSettings> optionsMonitor,
        IProcessEventWatcher eventWatcher,
        IProcessEventRouter router,
        IKeyRepeatSettingsService settingsService,
        IAppSettingsChangeHandler settingsHandler,
        ILogger<StartupWatcherTrigger> logger)
    {
        _optionsMonitor = optionsMonitor;
        _eventWatcher = eventWatcher;
        _router = router;
        _settingsService = settingsService;
        _settingsHandler = settingsHandler;
        _logger = logger;
    }


    public void Trigger()
    {
        ApplySettings(_optionsMonitor.CurrentValue);

        _optionsMonitor.OnChange(settings =>
        {
            var now = DateTime.UtcNow;
            if (now - _lastUpdate < DebounceWindow)
            {
                _logger.LogInformation("Ignoring duplicate OnChange trigger.");
                return;
            }

            _lastUpdate = now;
            ApplySettings(settings);
        });
    }

    private void ApplySettings(AppSettings settings)
    {
        _settingsHandler.OnSettingsChanged(settings);
        _eventWatcher.OnSettingsChanged(settings);
        _settingsService.UpdateRunningState(_router.IsRunning);
    }
}