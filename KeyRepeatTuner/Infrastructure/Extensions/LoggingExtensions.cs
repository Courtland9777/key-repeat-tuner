using Serilog;

namespace KeyRepeatTuner.Infrastructure.Extensions;

public static class LoggingExtensions
{
    public static HostApplicationBuilder ConfigureSerilog(this HostApplicationBuilder builder)
    {
        var configuration = builder.Configuration;

        var loggerConfig = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
#if DEBUG
            .WriteTo.File("logs/process_monitor.log", rollingInterval: RollingInterval.Day)
#endif
            .WriteTo.EventLog("KeyRepeatTuner", manageEventSource: true);

        builder.Logging.ClearProviders();
        builder.Logging.AddSerilog(loggerConfig.CreateLogger());

        return builder;
    }

    public static LoggerConfiguration ReadFromUserScopedAppSettings(
        this LoggerConfiguration loggerConfiguration,
        string appName)
    {
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            appName);

        var userScopedConfigPath = Path.Combine(appDataPath, "appsettings.json");
        var exeConfigPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");

        if (!File.Exists(userScopedConfigPath))
        {
            Directory.CreateDirectory(appDataPath);
            if (File.Exists(exeConfigPath))
            {
                File.Copy(exeConfigPath, userScopedConfigPath);
            }
            else
            {
                Console.Error.WriteLine("Missing appsettings.json: not found in AppData or EXE path.");
                throw new FileNotFoundException("appsettings.json missing for logger bootstrapping.");
            }
        }

        var configuration = new ConfigurationBuilder()
            .SetBasePath(appDataPath)
            .AddJsonFile("appsettings.json", false, true)
            .Build();

        return loggerConfiguration.ReadFrom.Configuration(configuration);
    }
}