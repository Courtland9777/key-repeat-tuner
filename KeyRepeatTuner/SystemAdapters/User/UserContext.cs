using System.Security.Principal;
using KeyRepeatTuner.SystemAdapters.Interfaces;

namespace KeyRepeatTuner.SystemAdapters.User;

internal sealed class UserContext : IUserContext
{
    public bool IsAdministrator()
    {
        using var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }
}