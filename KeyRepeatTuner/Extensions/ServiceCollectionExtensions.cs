using FluentValidation;
using KeyRepeatTuner.Configuration;
using KeyRepeatTuner.Configuration.Validation;
using KeyRepeatTuner.Configuration.ValueObjects;
using KeyRepeatTuner.Interfaces;
using KeyRepeatTuner.Services;
using KeyRepeatTuner.SystemAdapters.Interfaces;
using KeyRepeatTuner.SystemAdapters.Wrappers;
using Microsoft.Extensions.Options;

namespace KeyRepeatTuner.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddValidatedAppSettings(this IHostApplicationBuilder builder)
    {
        builder.Services
            .AddOptions<AppSettingsDto>()
            .Bind(builder.Configuration.GetSection("AppSettings"))
            .ValidateOnStart();

        builder.Services.AddSingleton<IOptionsMonitor<AppSettings>>(sp =>
        {
            var dtoMonitor = sp.GetRequiredService<IOptionsMonitor<AppSettingsDto>>();
            var validator = sp.GetRequiredService<IValidator<AppSettings>>();

            return new TransformingOptionsMonitor<AppSettingsDto, AppSettings>(
                dtoMonitor,
                dto =>
                {
                    var mapped = new AppSettings
                    {
                        ProcessNames = dto.ProcessNames?.Select(name => new ProcessName(name)).ToList()
                                       ?? throw new InvalidOperationException("ProcessNames must be set"),
                        KeyRepeat = dto.KeyRepeat ?? throw new InvalidOperationException("KeyRepeat settings missing")
                    };

                    var result = validator.Validate(mapped);
                    if (!result.IsValid)
                        throw new OptionsValidationException(nameof(AppSettings), typeof(AppSettings),
                            result.Errors.Select(e => e.ErrorMessage));

                    return mapped;
                });
        });
    }


    public static void AddApplicationServices(this IHostApplicationBuilder builder)
    {
        builder.Services.AddSingleton<IValidator<AppSettings>, AppSettingsValidator>();

        builder.Services.AddInfrastructureServices();
    }


    private static void AddInfrastructureServices(this IServiceCollection services)
    {
        services
            .AddSingleton<IKeyboardRegistryReader, KeyboardRegistryReader>()
            .AddSingleton<ProcessEventWatcher>()
            .AddSingleton<IProcessEventWatcher>(sp => sp.GetRequiredService<ProcessEventWatcher>())
            .AddSingleton<StartupWatcherTrigger>()
            .AddSingleton<IProcessEventRouter, ProcessStateTracker>()
            .AddSingleton<IManagementEventWatcherFactory, ManagementEventWatcherFactory>()
            .AddSingleton<ProcessStateTracker>()
            .AddSingleton<IKeyboardSettingsApplier, KeyboardSettingsApplier>()
            .AddSingleton<IKeyRepeatApplier, KeyRepeatApplier>()
            .AddSingleton<KeyRepeatModeResolver>()
            .AddSingleton<IKeyRepeatSettingsService, KeyRepeatModeCoordinator>()
            .AddSingleton<IAppSettingsChangeHandler>(sp =>
                (IAppSettingsChangeHandler)sp.GetRequiredService<IKeyRepeatSettingsService>())
            .AddSingleton<IProcessProvider, ProcessProvider>()
            .AddSingleton<IUserContext, UserContext>()
            .AddHostedService<WatcherHostedService>();
    }
}