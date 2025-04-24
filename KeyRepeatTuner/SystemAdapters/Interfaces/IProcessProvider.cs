namespace KeyRepeatTuner.SystemAdapters.Interfaces;

public interface IProcessProvider
{
    IEnumerable<int> GetProcessIdsByName(string name);
}