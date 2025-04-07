using KeyRepeatTuner.Configuration;
using KeyRepeatTuner.Events;
using KeyRepeatTuner.Interfaces;
using MediatR;
using Microsoft.Extensions.Options;

namespace KeyRepeatTuner.Services;

public class StartupWatcherTrigger : INotificationHandler<AppStartupInitiated>
{
    private readonly IProcessEventWatcher _eventWatcher;
    private readonly IOptionsMonitor<AppSettings> _optionsMonitor;

    public StartupWatcherTrigger(
        IOptionsMonitor<AppSettings> optionsMonitor,
        IProcessEventWatcher eventWatcher
    )
    {
        _optionsMonitor = optionsMonitor;
        _eventWatcher = eventWatcher;
    }

    public Task Handle(AppStartupInitiated notification, CancellationToken cancellationToken)
    {
        var settings = _optionsMonitor.CurrentValue;
        _eventWatcher.OnSettingsChanged(settings);
        return Task.CompletedTask;
    }
}