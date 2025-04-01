using StarCraftKeyManager.Configuration;

namespace StarCraftKeyManager.Interfaces;

public interface IAppSettingsChangeHandler
{
    void OnSettingsChanged(AppSettings newSettings);
}