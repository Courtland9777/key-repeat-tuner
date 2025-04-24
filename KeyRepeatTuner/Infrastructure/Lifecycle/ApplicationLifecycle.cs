namespace KeyRepeatTuner.Infrastructure.Lifecycle;

public static class ApplicationLifecycle
{
    public static void RegisterShutdownLogging(IHost app)
    {
        var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
        var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Shutdown");
        lifetime.ApplicationStopping.Register(() => logger.LogInformation("Application stopping..."));
    }
}