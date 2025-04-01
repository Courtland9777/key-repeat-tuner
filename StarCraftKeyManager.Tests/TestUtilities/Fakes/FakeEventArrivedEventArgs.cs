using StarCraftKeyManager.SystemAdapters.Interfaces;

namespace StarCraftKeyManager.Tests.TestUtilities.Fakes;

public class FakeEventArrivedEventArgs : IEventArrivedEventArgs
{
    private readonly int _pid;

    public FakeEventArrivedEventArgs(int pid)
    {
        _pid = pid;
    }

    public int GetProcessId()
    {
        return _pid;
    }
}