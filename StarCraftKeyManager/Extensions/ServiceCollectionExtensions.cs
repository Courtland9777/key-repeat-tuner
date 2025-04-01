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

        builder.Services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssemblies(typeof(ProcessStarted).Assembly));

        builder.Services.AddInfrastructureServices();
        builder.Services.AddMonitoringServices();
    }

    private static void AddInfrastructureServices(this IServiceCollection services)
    {
        services
            .AddSingleton<IKeyboardSettingsApplier, KeyboardSettingsApplier>()
            .AddSingleton<IKeyRepeatSettingsService, KeyRepeatSettingsService>()
            .AddSingleton<IProcessProvider, ProcessProvider>()
            .AddSingleton<IUserContext, UserContext>()
            .AddSingleton<IProcessEventWatcher, ProcessEventWatcher>()
            .AddSingleton<IManagementEventWatcherFactory, ManagementEventWatcherFactory>()
            .AddSingleton<ProcessStateTracker>();
    }

    private static void AddMonitoringServices(this IServiceCollection services)
    {
        services
            .AddSingleton<IHealthCheck, ProcessWatcherHealthCheck>()
            .AddHealthChecks()
            .AddCheck<ProcessWatcherHealthCheck>("process_watcher", tags: ["ready", "live"]);
    }
}