namespace KeyRepeatTuner.Extensions;

public static class HostBuilderExtensions
{
    public static void SetServiceName(this IHostApplicationBuilder builder)
    {
        builder.Services.Configure<WindowsServiceLifetimeOptions>(options =>
            options.ServiceName = "Key Repeat Tuner");
    }
}