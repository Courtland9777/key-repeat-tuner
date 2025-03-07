using Serilog;
using StarCraftKeyManager;
using StarCraftKeyManager.Models;
using StarCraftKeyManager.Validators;
using System.Security.Principal;

if (!IsRunningAsAdmin())
{
    Log.Error("Application is not running as administrator. Please run as administrator.");
    Environment.Exit(1);
}

using var host = Host.CreateDefaultBuilder()
    .UseWindowsService(options =>
    {
        options.ServiceName = "StarCraft Key Manager";
    })
    .ConfigureAppConfiguration((context, config) =>
    {
        config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
    })
    .UseSerilog((context, services, loggerConfig) =>
    {
        loggerConfig.ReadFrom.Configuration(context.Configuration);
    })
    .ConfigureServices((context, services) =>
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
        services.AddSingleton<ProcessMonitorService>();
        services.AddHostedService(provider => provider.GetRequiredService<ProcessMonitorService>());
    })
    .Build();

Log.Information("Process Monitor Service Started. Press Ctrl+C to exit.");
await host.RunAsync();
return;

static bool IsRunningAsAdmin()
{
    using var identity = WindowsIdentity.GetCurrent();
    var principal = new WindowsPrincipal(identity);
    return principal.IsInRole(WindowsBuiltInRole.Administrator);
}