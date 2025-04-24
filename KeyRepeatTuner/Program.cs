using KeyRepeatTuner.Infrastructure.Extensions;
using KeyRepeatTuner.Infrastructure.Lifecycle;
using KeyRepeatTuner.SystemAdapters.User;
using Serilog;

try
{
    using var app = HostBuilderExtensions.BuildKeyRepeatTunerApp(args);

    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    AppDomain.CurrentDomain.UnhandledException += (_, e) =>
    {
        logger.LogCritical((Exception)e.ExceptionObject, "Unhandled exception in AppDomain");
    };

    if (!AppStartupGuards.ValidateAdministratorPrivileges(app.Services))
    {
        Environment.ExitCode = 1;
        return;
    }

    ApplicationLifecycle.RegisterShutdownLogging(app);
    logger.LogInformation("Process Monitor Service Started. Press Ctrl+C to exit.");

    await app.RunAsync();

    logger.LogInformation("Application shut down gracefully (exit code 0).");
    Environment.ExitCode = 0;
}
catch (Exception ex)
{
    using var loggerFactory = LoggerFactory.Create(builder => builder.AddSerilog());
    var logger = loggerFactory.CreateLogger<Program>();
    logger.LogCritical(ex, "Application terminated unexpectedly (exit code 1)");
    Environment.ExitCode = 1;
}