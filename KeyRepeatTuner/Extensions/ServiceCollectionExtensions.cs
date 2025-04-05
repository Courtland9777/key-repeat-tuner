using FluentValidation;
using KeyRepeatTuner.Configuration;
using KeyRepeatTuner.Configuration.Validation;
using KeyRepeatTuner.Configuration.ValueObjects;
using KeyRepeatTuner.Events;
using KeyRepeatTuner.Health;
using KeyRepeatTuner.Interfaces;
using KeyRepeatTuner.Services;
using KeyRepeatTuner.SystemAdapters.Interfaces;
using KeyRepeatTuner.SystemAdapters.Wrappers;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace KeyRepeatTuner.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddValidatedAppSettings(this IHostApplicationBuilder builder)
    {
        builder.Services.AddSingleton<IOptionsMonitor<AppSettings>>(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var validator = sp.GetRequiredService<IValidator<AppSettings>>();

            var dto = config.GetSection("AppSettings").Get<AppSettingsDto>()
                      ?? throw new InvalidOperationException("AppSettings section missing");

            var instance = new AppSettings
            {
                ProcessNames = dto.ProcessNames?.Select(name => new ProcessName(name)).ToList()
                               ?? throw new InvalidOperationException("ProcessNames must be set"),
                KeyRepeat = dto.KeyRepeat ?? throw new InvalidOperationException("KeyRepeat settings missing")
            };

            var validation = validator.Validate(instance);
            if (validation.IsValid) return new SimpleOptionsMonitor<AppSettings>(instance);
            var errors = validation.Errors.Select(e => $"{e.PropertyName} - {e.ErrorMessage}");
            throw new OptionsValidationException(nameof(AppSettings), typeof(AppSettings), errors);
        });

        builder.Services.Configure<AppSettingsDto>(builder.Configuration.GetSection("AppSettings"));
    }


    public static void AddApplicationServices(this IHostApplicationBuilder builder)
    {
        builder.Services.AddSingleton<IValidator<AppSettings>, AppSettingsValidator>();
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
            .AddSingleton<ProcessEventWatcher>()
            .AddSingleton<IProcessEventWatcher>(sp => sp.GetRequiredService<ProcessEventWatcher>())
            .AddSingleton<IManagementEventWatcherFactory, ManagementEventWatcherFactory>()
            .AddSingleton<ProcessStateTracker>()
            .AddSingleton<IKeyboardSettingsApplier, KeyboardSettingsApplier>()
            .AddSingleton<IKeyRepeatSettingsService, KeyRepeatSettingsService>()
            .AddSingleton<IProcessProvider, ProcessProvider>()
            .AddSingleton<IUserContext, UserContext>();
    }


    private static void AddMonitoringServices(this IServiceCollection services)
    {
        services
            .AddSingleton<IHealthCheck, ProcessWatcherHealthCheck>()
            .AddHealthChecks()
            .AddCheck<ProcessWatcherHealthCheck>("process_watcher", tags: ["ready", "live"]);
    }

    private record AppSettingsDto(
        List<string>? ProcessNames,
        KeyRepeatSettings? KeyRepeat
    );
}