using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Options;
using Serilog;
using StarCraftKeyManager.Configuration;
using StarCraftKeyManager.Configuration.Validation;
using StarCraftKeyManager.Events;
using StarCraftKeyManager.Interfaces;
using StarCraftKeyManager.Services;
using StarCraftKeyManager.SystemAdapters.Interfaces;
using StarCraftKeyManager.SystemAdapters.Wrappers;

namespace StarCraftKeyManager.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddAppSettingsJson(this IHostApplicationBuilder builder)
    {
        builder.Services.Configure<AppSettings>(builder.Configuration.GetSection("AppSettings"));
    }

    public static void AddApplicationServices(this IHostApplicationBuilder builder)
    {
        builder.Services.AddValidatorsFromAssemblyContaining<AppSettingsValidator>();
        builder.Services.AddSingleton<AppSettingsChangeValidator>();
        builder.Services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssemblies(typeof(ProcessStarted).Assembly));

        using var serviceProvider = builder.Services.BuildServiceProvider();
        var optionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<AppSettings>>();
        var validator = serviceProvider.GetRequiredService<IValidator<AppSettings>>();
        var changeValidator = new AppSettingsChangeValidator(validator);

        // Initial validation
        var appSettings = optionsMonitor.CurrentValue;
        var validationResults = validator.Validate(appSettings);
        if (!validationResults.IsValid)
        {
            LogValidationErrors(validationResults);
            Log.Error("Configuration is invalid. Application startup aborted due to {ErrorCount} validation errors.",
                validationResults.Errors.Count);

            throw new InvalidOperationException(
                "Invalid AppSettings detected. Please correct the errors and restart the application.");
        }

        // Runtime validation
        optionsMonitor.OnChange(newSettings =>
        {
            if (!changeValidator.Validate(newSettings))
                Log.Warning("Rejected invalid configuration update at runtime.");
        });

        RegisterServices(builder.Services);
    }


    private static void LogValidationErrors(ValidationResult validationResults)
    {
        foreach (var failure in validationResults.Errors)
            Log.Error("Validation error: {PropertyName} - {ErrorMessage}",
                failure.PropertyName, failure.ErrorMessage);
    }

    private static void RegisterServices(IServiceCollection services)
    {
        services.AddSingleton<ProcessMonitorService>();
        services.AddSingleton<IProcessEventWatcher, ProcessEventWatcher>();
        services.AddSingleton<IManagementEventWatcherFactory, ManagementEventWatcherFactory>();
        services.AddSingleton<IKeyboardSettingsApplier, KeyboardSettingsApplier>();
        services.AddSingleton<IKeyRepeatSettingsService, KeyRepeatSettingsService>();
        services.AddSingleton<IProcessProvider, ProcessProvider>();
        services.AddSingleton<IUserContext, UserContext>();
    }
}