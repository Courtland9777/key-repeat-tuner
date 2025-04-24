using KeyRepeatTuner.SystemAdapters.Interfaces;

namespace KeyRepeatTuner.SystemAdapters.User;

public static class AppStartupGuards
{
    public static bool ValidateAdministratorPrivileges(IServiceProvider services)
    {
        var userContext = services.GetRequiredService<IUserContext>();
        var skipAdmin = Environment.GetEnvironmentVariable("SKIP_ADMIN_CHECK") == "true";

        if (skipAdmin || !IsNotRunningUnderTest() || userContext.IsAdministrator()) return true;
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");
        logger.LogError("Application is not running as administrator. Please run as administrator.");
        return false;
    }

    private static bool IsNotRunningUnderTest()
    {
        return AppDomain.CurrentDomain.GetAssemblies()
                   .All(a => !(a.FullName?.StartsWith("xunit", StringComparison.OrdinalIgnoreCase) ?? false)) &&
               !AppDomain.CurrentDomain.FriendlyName.Contains("testhost", StringComparison.OrdinalIgnoreCase);
    }
}