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
}