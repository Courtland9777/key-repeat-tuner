using KeyRepeatTuner.Infrastructure.ServiceCollection;

namespace KeyRepeatTuner.Infrastructure.Extensions;

public static class HostBuilderExtensions
{
    private static HostApplicationBuilder SetServiceName(this HostApplicationBuilder builder)
    {
        builder.Services.Configure<WindowsServiceLifetimeOptions>(options =>
            options.ServiceName = "Key Repeat Tuner");

        return builder;
    }

    public static IHost BuildKeyRepeatTunerApp(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args)
            .UseUserScopedAppSettings("KeyRepeatTuner")
            .ConfigureSerilog()
            .SetServiceName()
            .AddValidatedAppSettings()
            .AddApplicationServices();

        return builder.Build();
    }

    private static HostApplicationBuilder UseUserScopedAppSettings(this HostApplicationBuilder builder, string appName)
    {
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            appName);

        var userScopedConfig = Path.Combine(appDataPath, "appsettings.json");
        var exeConfig = Path.Combine(AppContext.BaseDirectory, "appsettings.json");

        if (!File.Exists(userScopedConfig))
        {
            Directory.CreateDirectory(appDataPath);

            if (File.Exists(exeConfig))
            {
                File.Copy(exeConfig, userScopedConfig);
            }
            else
            {
                Console.Error.WriteLine("Missing appsettings.json: not found in AppData or EXE path.");
                throw new FileNotFoundException("appsettings.json missing for configuration bootstrapping.");
            }
        }

        builder.Configuration
            .SetBasePath(appDataPath)
            .AddJsonFile("appsettings.json", false, true);

        return builder;
    }
}