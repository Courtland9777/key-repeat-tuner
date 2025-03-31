using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Options;
using Serilog;
using StarCraftKeyManager.Configuration;
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

        using var serviceProvider = builder.Services.BuildServiceProvider();
        var appSettings = serviceProvider.GetRequiredService<IOptions<AppSettings>>().Value;
        var validator = serviceProvider.GetRequiredService<IValidator<AppSettings>>();
        var validationResults = validator.Validate(appSettings);

        if (!validationResults.IsValid)
        {
            LogValidationErrors(validationResults);
            Log.Error("Using default key repeat settings due to invalid configuration.");
            throw new InvalidOperationException(
                "Invalid AppSettings detected. Please correct the errors and restart the application.");
        }

        RegisterServices(builder.Services, builder.Configuration);
    }

    private static void LogValidationErrors(ValidationResult validationResults)
    {
        foreach (var failure in validationResults.Errors)
            Log.Warning("Invalid configuration: {PropertyName} - {ErrorMessage}",
                failure.PropertyName, failure.ErrorMessage);
    }

    private static void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AppSettings>(configuration.GetSection(nameof(AppSettings)));
        services.AddSingleton<ProcessMonitorService>();
        services.AddHostedService(provider => provider.GetRequiredService<ProcessMonitorService>());
        services.AddSingleton<IProcessEventWatcher, ProcessEventWatcher>();
        services.AddSingleton<IManagementEventWatcherFactory, ManagementEventWatcherFactory>();
        services.AddSingleton<IKeyboardSettingsApplier, KeyboardSettingsApplier>();
        services.AddSingleton<IProcessProvider, ProcessProvider>();
        services.AddSingleton<IUserContext, UserContext>();
    }
}