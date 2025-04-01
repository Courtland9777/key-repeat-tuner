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

    public AppSettingsRuntimeCoordinator(
        IValidator<AppSettings> validator,
        IEnumerable<IAppSettingsChangeHandler> handlers,
        IOptionsMonitor<AppSettings> optionsMonitor,
        ILogger<AppSettingsRuntimeCoordinator> logger)
    {
        _validator = validator;
        _handlers = handlers;
        _logger = logger;

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

        foreach (var handler in _handlers)
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