namespace KeyRepeatTuner.Infrastructure.Extensions;

public static class HostBuilderExtensions
{
    public static void SetServiceName(this IHostApplicationBuilder builder)
    {
        builder.Services.Configure<WindowsServiceLifetimeOptions>(options =>
            options.ServiceName = "Key Repeat Tuner");
    }

    public static HostApplicationBuilder UseUserScopedAppSettings(this HostApplicationBuilder builder, string appName)
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