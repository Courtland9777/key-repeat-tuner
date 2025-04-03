namespace KeyRepeatTuner.Interfaces;

public interface IProcessNamesChangeHandler : IAppSettingsChangeHandler
{
    void OnProcessNamesChanged(List<string> added, List<string> removed);
}