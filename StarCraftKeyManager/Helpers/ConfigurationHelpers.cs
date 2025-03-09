using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using Serilog;
using StarCraftKeyManager.Interfaces;
using StarCraftKeyManager.Models;
using StarCraftKeyManager.Services;
using StarCraftKeyManager.Validators;

namespace StarCraftKeyManager.Helpers;

public static class ConfigurationHelpers
{
    //Create an extension method to add appsetting.json to the host
    public static void AddAppSettingsJson(this IHostBuilder hostBuilder)
    {
        hostBuilder.ConfigureAppConfiguration((hostingContext, config) =>
        {
            config.AddJsonFile("appsettings.json", optional: true);
        });
    }

    public static void SetServiceName(this IHostBuilder hostBuilder)
    {
        hostBuilder.UseWindowsService(options =>
        {
            options.ServiceName = "StarCraft Key Manager";
        });
    }

    public static void ConfigureSerilog(this IHostBuilder hostBuilder)
    {
        hostBuilder.UseSerilog((context, services, loggerConfig) =>
        {
            loggerConfig.ReadFrom.Configuration(context.Configuration);
        });
    }

    public static void AddApplicationServices(this IHostBuilder hostBuilder)
    {
        hostBuilder.ConfigureServices((context, services) =>
        {
            // Load configuration and validate it before registering
            var configuration = context.Configuration;
            var appSettings = configuration.Get<AppSettings>() ?? new AppSettings();

            // Validate configuration
            var validator = new AppSettingsValidator();
            var results = validator.Validate(appSettings);

            if (!results.IsValid)
            {
                foreach (var failure in results.Errors)
                {
                    Log.Warning("Invalid configuration: {PropertyName} - {ErrorMessage}",
                        failure.PropertyName, failure.ErrorMessage);
                }

                Log.Warning("Using default key repeat settings due to invalid configuration.");
            }

            // Register configuration and services
            services.Configure<AppSettings>(configuration);
            services.AddSingleton<IProcessMonitorService, ProcessMonitorService>();
            services.AddHostedService(provider => provider.GetRequiredService<ProcessMonitorService>());
        });
    }

    public static bool IsRunningAsAdmin()
    {
        using var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }
}