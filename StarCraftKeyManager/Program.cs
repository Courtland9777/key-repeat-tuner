using Serilog;
using StarCraftKeyManager.Adapters;
using StarCraftKeyManager.Helpers;

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(new ConfigurationBuilder().AddJsonFile("appsettings.json", true, true).Build())
    .CreateLogger();

try
{
    var builder = Host.CreateApplicationBuilder(args);
    builder.SetServiceName();
    builder.AddAppSettingsJson();
    builder.ConfigureSerilog();
    builder.AddApplicationServices();

    using var app = builder.Build();

    var userContext = app.Services.GetRequiredService<IUserContext>();
    if (!userContext.IsAdministrator())
    {
        Log.Error("Application is not running as administrator. Please run as administrator.");
        Environment.ExitCode = 1;
        return;
    }

    var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
    lifetime.ApplicationStopping.Register(() => Log.Information("Application stopping..."));

    Log.Information("Process Monitor Service Started. Press Ctrl+C to exit.");
    await app.RunAsync();

    Log.Information("Application shut down gracefully (exit code 0).");
    Environment.ExitCode = 0;
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly (exit code 1)");
    Environment.ExitCode = 1;
}
finally
{
    Log.CloseAndFlush();
}