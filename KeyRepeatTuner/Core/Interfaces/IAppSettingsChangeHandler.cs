using KeyRepeatTuner.Configuration;

namespace KeyRepeatTuner.Core.Interfaces;

public interface IAppSettingsChangeHandler
{
    void OnSettingsChanged(AppSettings newSettings);
}