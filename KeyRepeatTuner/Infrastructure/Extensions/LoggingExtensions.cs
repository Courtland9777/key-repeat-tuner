using Serilog;

namespace KeyRepeatTuner.Infrastructure.Extensions;

public static class LoggingExtensions
{
    public static void ConfigureSerilog(this IHostApplicationBuilder builder)
    {
        builder.Logging.ClearProviders();
        builder.Logging.AddSerilog(new LoggerConfiguration()
            .ReadFrom.Configuration(builder.Configuration)
            .CreateLogger());
    }
}