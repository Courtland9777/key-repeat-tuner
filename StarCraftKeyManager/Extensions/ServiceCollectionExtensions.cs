using FluentValidation;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using StarCraftKeyManager.Configuration;
using StarCraftKeyManager.Configuration.Validation;
using StarCraftKeyManager.Events;
using StarCraftKeyManager.Health;
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
        builder.Services.AddSingleton<IAppSettingsChangeHandler, KeyRepeatSettingsService>();
        builder.Services.AddHealthChecks()
            .AddCheck<ProcessWatcherHealthCheck>("Process Watcher");
        builder.Services.AddSingleton<IHealthCheck, ProcessWatcherHealthCheck>();

        builder.Services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssemblies(typeof(ProcessStarted).Assembly));

        RegisterServices(builder.Services);
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
        services.AddHostedService<AppSettingsStartupValidator>();
    }
}