using Microsoft.Extensions.Options;

namespace StarCraftKeyManager.Tests.MoqExtensions;

public class TestOptionsMonitor<T> : IOptionsMonitor<T>
{
    private readonly Dictionary<string, T> _namedValues = new();
    private Action<T, string?>? _onChange;

    public TestOptionsMonitor(T initialValue)
    {
        CurrentValue = initialValue;
        _namedValues[""] = initialValue; // default name
    }

    public T CurrentValue { get; private set; }

    public T Get(string? name)
    {
        name ??= "";
        return _namedValues.TryGetValue(name, out var value)
            ? value
            : CurrentValue;
    }

    public IDisposable OnChange(Action<T, string?> listener)
    {
        _onChange = listener;
        return new DummyDisposable();
    }

    public void TriggerChange(T newValue, string? name = null)
    {
        name ??= "";
        _namedValues[name] = newValue;
        CurrentValue = newValue;
        _onChange?.Invoke(newValue, name);
    }

    private class DummyDisposable : IDisposable
    {
        public void Dispose()
        {
        }
    }
}