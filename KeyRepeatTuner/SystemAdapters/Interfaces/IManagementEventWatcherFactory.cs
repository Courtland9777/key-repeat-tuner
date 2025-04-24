namespace KeyRepeatTuner.SystemAdapters.Interfaces;

public interface IManagementEventWatcherFactory
{
    IManagementEventWatcher Create(string wqlQuery);
}