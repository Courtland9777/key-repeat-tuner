using System.Security.Principal;
using FluentValidation.Results;
using Serilog;
using StarCraftKeyManager.Interfaces;
using StarCraftKeyManager.Models;
using StarCraftKeyManager.Services;
using StarCraftKeyManager.Validators;

namespace StarCraftKeyManager.Helpers;

public static class ConfigurationHelpers
{
    public static void AddAppSettingsJson(this IHostApplicationBuilder builder)
    {
        builder.Configuration.AddJsonFile("appsettings.json", true, true);
    }

    public static void SetServiceName(this IHostApplicationBuilder builder)
    {
        builder.Services.Configure<WindowsServiceLifetimeOptions>(options =>
            options.ServiceName = "StarCraft Key Manager");
    }

    public static void ConfigureSerilog(this IHostApplicationBuilder builder)
    {
        builder.Logging.ClearProviders(); // Remove default logging providers
        builder.Logging.AddSerilog(new LoggerConfiguration()
            .ReadFrom.Configuration(builder.Configuration)
            .CreateLogger());
    }

    public static void AddApplicationServices(this IHostApplicationBuilder builder)
    {
        // Load and validate configuration
        var appSettings = builder.Configuration.Get<AppSettings>() ?? new AppSettings();
        var validator = new AppSettingsValidator();
        var validationResults = validator.Validate(appSettings);

        if (!validationResults.IsValid)
        {
            LogValidationErrors(validationResults);
            Log.Warning("Using default key repeat settings due to invalid configuration.");
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
        services.Configure<AppSettings>(configuration);
        services.AddSingleton<IProcessMonitorService, ProcessMonitorService>();
        services.AddHostedService(provider => provider.GetRequiredService<ProcessMonitorService>());
    }

    public static bool IsRunningAsAdmin()
    {
        using var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }
}