using KeyRepeatTuner.Configuration;
using KeyRepeatTuner.Interfaces;
using Microsoft.Extensions.Options;

namespace KeyRepeatTuner.Services;

public class StartupWatcherTrigger
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
        var settings = _optionsMonitor.CurrentValue;
        _eventWatcher.OnSettingsChanged(settings);
        _router.OnStartup();
    }
}