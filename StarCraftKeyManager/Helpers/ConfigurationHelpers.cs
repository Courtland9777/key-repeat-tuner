﻿using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Options;
using Serilog;
using StarCraftKeyManager.Adapters;
using StarCraftKeyManager.Interfaces;
using StarCraftKeyManager.Models;
using StarCraftKeyManager.Services;
using StarCraftKeyManager.Validators;
using StarCraftKeyManager.Wrappers;

namespace StarCraftKeyManager.Helpers;

public static class ConfigurationHelpers
{
    public static void AddAppSettingsJson(this IHostApplicationBuilder builder)
    {
        builder.Services.Configure<AppSettings>(builder.Configuration.GetSection("AppSettings"));
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
        builder.Services.AddValidatorsFromAssemblyContaining<AppSettingsValidator>();

        // Validate configuration
        using var serviceProvider = builder.Services.BuildServiceProvider();
        var appSettings = serviceProvider.GetRequiredService<IOptions<AppSettings>>().Value;
        var validator = serviceProvider.GetRequiredService<IValidator<AppSettings>>();
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
        services.Configure<AppSettings>(configuration.GetSection(nameof(AppSettings)));
        services.AddSingleton<ProcessMonitorService>();
        services.AddHostedService(provider => provider.GetRequiredService<ProcessMonitorService>());
        services.AddSingleton<IProcessEventWatcher, ProcessEventWatcher>();
        services.AddSingleton<IEventWatcherFactory, EventWatcherFactory>();
        services.AddSingleton<IEventLogQueryBuilder, SecurityAuditQueryBuilder>();
        services.AddSingleton<IKeyboardSettingsApplier, KeyboardSettingsApplier>();
        services.AddSingleton<IProcessProvider, ProcessProvider>();
        services.AddSingleton<IUserContext, UserContext>();
    }
}