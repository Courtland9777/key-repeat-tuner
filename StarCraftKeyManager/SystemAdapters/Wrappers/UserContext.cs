using System.Security.Principal;
using StarCraftKeyManager.Adapters;

namespace StarCraftKeyManager.Wrappers;

internal sealed class UserContext : IUserContext
{
    public bool IsAdministrator()
    {
        using var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }
}