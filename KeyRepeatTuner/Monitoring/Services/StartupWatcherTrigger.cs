using KeyRepeatTuner.Configuration;
using KeyRepeatTuner.Core.Interfaces;
using KeyRepeatTuner.Monitoring.Interfaces;
using Microsoft.Extensions.Options;

namespace KeyRepeatTuner.Monitoring.Services;

public class StartupWatcherTrigger : IStartupWatcherTrigger
{
    private readonly IProcessEventWatcher _eventWatcher;
    private readonly IOptionsMonitor<AppSettings> _optionsMonitor;
    private readonly IProcessEventRouter _router;

    public StartupWatcherTrigger(
        IOptionsMonitor<AppSettings> optionsMonitor,
        IProcessEventWatcher eventWatcher,
        IProcessEventRouter router)
    {
        _optionsMonitor = optionsMonitor;
        _eventWatcher = eventWatcher;
        _router = router;
    }

    public void Trigger()
    {
        var current = _optionsMonitor.CurrentValue;

        _eventWatcher.OnSettingsChanged(current);
        _router.OnStartup();

        _optionsMonitor.OnChange(settings =>
        {
            _eventWatcher.OnSettingsChanged(settings);
            _router.OnStartup();
        });
    }
}