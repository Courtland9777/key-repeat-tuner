using FluentValidation;
using KeyRepeatTuner.Configuration;
using KeyRepeatTuner.Configuration.Dto;
using KeyRepeatTuner.Configuration.Mapping;
using KeyRepeatTuner.Configuration.Validation;
using KeyRepeatTuner.Core.Interfaces;
using KeyRepeatTuner.Core.Services;
using KeyRepeatTuner.Monitoring.Interfaces;
using KeyRepeatTuner.Monitoring.Services;
using KeyRepeatTuner.SystemAdapters.Interfaces;
using KeyRepeatTuner.SystemAdapters.Keyboard;
using KeyRepeatTuner.SystemAdapters.Processes;
using KeyRepeatTuner.SystemAdapters.User;
using KeyRepeatTuner.SystemAdapters.WMI;
using Microsoft.Extensions.Options;

namespace KeyRepeatTuner.Infrastructure.ServiceCollection;

public static class ServiceCollectionExtensions
{
    public static HostApplicationBuilder AddValidatedAppSettings(this HostApplicationBuilder builder)
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
                    var mapped = AppSettingsMapper.ToDomain(dto);

                    var result = validator.Validate(mapped);
                    if (!result.IsValid)
                        throw new OptionsValidationException(nameof(AppSettings), typeof(AppSettings),
                            result.Errors.Select(e => e.ErrorMessage));

                    return mapped;
                });
        });

        return builder;
    }


    public static HostApplicationBuilder AddApplicationServices(this HostApplicationBuilder builder)
    {
        builder.Services.AddSingleton<IValidator<AppSettings>, AppSettingsValidator>();

        builder.Services.AddInfrastructureServices();
        return builder;
    }


    private static void AddInfrastructureServices(this IServiceCollection services)
    {
        services
            .AddSingleton<IKeyboardRegistryReader, KeyboardRegistryReader>()
            .AddSingleton<ProcessEventWatcher>()
            .AddSingleton<IProcessEventWatcher>(sp => sp.GetRequiredService<ProcessEventWatcher>())
            .AddSingleton<IStartupWatcherTrigger, StartupWatcherTrigger>()
            .AddSingleton<IProcessEventRouter, ProcessStateTracker>()
            .AddSingleton<IManagementEventWatcherFactory, ManagementEventWatcherFactory>()
            .AddSingleton<ProcessStateTracker>()
            .AddSingleton<IKeyboardSettingsApplier, KeyboardSettingsApplier>()
            .AddSingleton<IKeyRepeatApplier, KeyRepeatApplier>()
            .AddSingleton<IKeyRepeatModeResolver, KeyRepeatModeResolver>()
            .AddSingleton<IKeyRepeatSettingsService, KeyRepeatModeCoordinator>()
            .AddSingleton<IAppSettingsChangeHandler>(sp =>
                (IAppSettingsChangeHandler)sp.GetRequiredService<IKeyRepeatSettingsService>())
            .AddSingleton<IProcessProvider, ProcessProvider>()
            .AddSingleton<IUserContext, UserContext>()
            .AddHostedService<WatcherHostedService>();
    }
}