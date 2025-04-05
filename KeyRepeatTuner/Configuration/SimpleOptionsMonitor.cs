using Microsoft.Extensions.Options;

namespace KeyRepeatTuner.Configuration;

public class SimpleOptionsMonitor<T> : IOptionsMonitor<T>
{
    public SimpleOptionsMonitor(T currentValue)
    {
        CurrentValue = currentValue;
    }

    public T CurrentValue { get; }

    public T Get(string? name)
    {
        return CurrentValue;
    }

    public IDisposable OnChange(Action<T, string> listener)
    {
        return new NoopDisposable();
    }

    private class NoopDisposable : IDisposable
    {
        public void Dispose()
        {
        }
    }
}