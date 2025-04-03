using KeyRepeatTuner.Configuration;

namespace KeyRepeatTuner.Interfaces;

public interface IAppSettingsChangeHandler
{
    void OnSettingsChanged(AppSettings newSettings);
}