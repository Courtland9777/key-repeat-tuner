using FluentValidation;
using MediatR;
using Microsoft.Extensions.Options;
using StarCraftKeyManager.Configuration;
using StarCraftKeyManager.Events;

namespace StarCraftKeyManager.Handlers;

public class AppSettingsStartupValidationHandler : INotificationHandler<AppStartupInitiated>
{
    private readonly ILogger<AppSettingsStartupValidationHandler> _logger;
    private readonly IOptions<AppSettings> _options;
    private readonly IValidator<AppSettings> _validator;

    public AppSettingsStartupValidationHandler(
        IOptions<AppSettings> options,
        IValidator<AppSettings> validator,
        ILogger<AppSettingsStartupValidationHandler> logger)
    {
        _options = options;
        _validator = validator;
        _logger = logger;
    }

    public Task Handle(AppStartupInitiated notification, CancellationToken cancellationToken)
    {
        var result = _validator.Validate(_options.Value);

        if (!result.IsValid)
        {
            foreach (var error in result.Errors)
                _logger.LogError("Startup validation error: {Property} - {Message}", error.PropertyName,
                    error.ErrorMessage);

            _logger.LogCritical("App startup aborted due to {ErrorCount} validation errors.", result.Errors.Count);
            throw new InvalidOperationException("Invalid AppSettings configuration.");
        }

        _logger.LogInformation("AppSettings startup validation passed.");
        return Task.CompletedTask;
    }
}