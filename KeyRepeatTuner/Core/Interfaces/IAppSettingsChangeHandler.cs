using KeyRepeatTuner.Configuration;

namespace KeyRepeatTuner.Core.Interfaces;

internal interface IAppSettingsChangeHandler
{
    void OnSettingsChanged(AppSettings newSettings);
}