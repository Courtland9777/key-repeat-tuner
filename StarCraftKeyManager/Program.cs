using System.Text.Json;
using MediatR;
using Serilog;
using StarCraftKeyManager.Configuration.Converters;
using StarCraftKeyManager.Events;
using StarCraftKeyManager.Extensions;
using StarCraftKeyManager.SystemAdapters.Interfaces;

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(new ConfigurationBuilder().AddJsonFile("appsettings.json", true, true).Build())
    .CreateLogger();

AppDomain.CurrentDomain.UnhandledException += (_, e) =>
{
    Log.Fatal((Exception)e.ExceptionObject, "Unhandled exception in AppDomain");
};

try
{
    var builder = Host.CreateApplicationBuilder(args);
    builder.ConfigureSerilog();
    builder.Services.Configure<JsonSerializerOptions>(options =>
    {
        options.Converters.Add(new ProcessNameJsonConverter());
    });
    builder.SetServiceName();
    builder.AddAppSettingsJson();
    builder.AddApplicationServices();

    using var app = builder.Build();

    var mediator = app.Services.GetRequiredService<IMediator>();
    await mediator.Publish(new AppStartupInitiated());
    var userContext = app.Services.GetRequiredService<IUserContext>();
    var skipAdmin = Environment.GetEnvironmentVariable("SKIP_ADMIN_CHECK") == "true";
    if (!skipAdmin && IsNotRunningUnderTest() && !userContext.IsAdministrator())
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

return;

static bool IsNotRunningUnderTest()
{
    return AppDomain.CurrentDomain.GetAssemblies()
               .All(a => !(a.FullName?.StartsWith("xunit", StringComparison.OrdinalIgnoreCase) ?? false)) &&
           !AppDomain.CurrentDomain.FriendlyName.Contains("testhost", StringComparison.OrdinalIgnoreCase);
}