using FluentValidation;
using Microsoft.Extensions.Options;
using StarCraftKeyManager.Configuration;
using StarCraftKeyManager.Interfaces;

namespace StarCraftKeyManager.Services;

internal sealed class AppSettingsRuntimeCoordinator
{
    private readonly IEnumerable<IAppSettingsChangeHandler> _handlers;
    private readonly ILogger<AppSettingsRuntimeCoordinator> _logger;
    private readonly IValidator<AppSettings> _validator;

    private List<string> _lastKnownProcessNames = [];

    public AppSettingsRuntimeCoordinator(
        IValidator<AppSettings> validator,
        IEnumerable<IAppSettingsChangeHandler> handlers,
        IOptionsMonitor<AppSettings> optionsMonitor,
        ILogger<AppSettingsRuntimeCoordinator> logger)
    {
        _validator = validator;
        _handlers = handlers;
        _logger = logger;

        _lastKnownProcessNames = [.. optionsMonitor.CurrentValue.ProcessNames.Select(p => p.Value)];
        optionsMonitor.OnChange(HandleSettingsChange);
    }

    private void HandleSettingsChange(AppSettings newSettings)
    {
        var result = _validator.Validate(newSettings);

        if (!result.IsValid)
        {
            foreach (var error in result.Errors)
                _logger.LogError("Validation failed: {Property} - {Message}", error.PropertyName, error.ErrorMessage);

            _logger.LogError("Rejected config update due to {ErrorCount} validation errors", result.Errors.Count);
            return;
        }

        var newProcessNames = newSettings.ProcessNames.Select(p => p.Value).ToList();

        if (!_lastKnownProcessNames.SequenceEqual(newProcessNames))
        {
            var removed = _lastKnownProcessNames.Except(newProcessNames).ToList();
            var added = newProcessNames.Except(_lastKnownProcessNames).ToList();

            _logger.LogInformation("Process name changes detected. Added: {Added}, Removed: {Removed}", added, removed);

            foreach (var handler in _handlers.OfType<IProcessNamesChangeHandler>())
                try
                {
                    handler.OnProcessNamesChanged(added, removed);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error notifying handler {HandlerType}", handler.GetType().Name);
                }

            _lastKnownProcessNames = newProcessNames;
        }

        foreach (var handler in _handlers.Except(_handlers.OfType<IProcessNamesChangeHandler>()))
            try
            {
                handler.OnSettingsChanged(newSettings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error notifying handler {HandlerType}", handler.GetType().Name);
            }
    }
}