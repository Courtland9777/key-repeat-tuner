using KeyRepeatTuner.Configuration;
using Microsoft.Extensions.Options;
using Xunit;

namespace KeyRepeatTuner.Tests.Configuration;

public class TransformingOptionsMonitorTests
{
    [Fact]
    public void CurrentValue_ShouldTransformCorrectly()
    {
        var monitor = new StubOptionsMonitor<string>("123");

        var wrapper = new TransformingOptionsMonitor<string, int>(monitor, int.Parse);

        Assert.Equal(123, wrapper.CurrentValue);
    }

    [Fact]
    public void Get_ShouldTransformCorrectly()
    {
        var monitor = new StubOptionsMonitor<string>("42");
        var wrapper = new TransformingOptionsMonitor<string, int>(monitor, int.Parse);

        var result = wrapper.Get("any-name");

        Assert.Equal(42, result);
    }

    [Fact]
    public void OnChange_ShouldInvokeWithTransformedValue()
    {
        var monitor = new StubOptionsMonitor<string>("0");
        var wrapper = new TransformingOptionsMonitor<string, int>(monitor, int.Parse);

        int? captured = null;
        wrapper.OnChange((newValue, _) => captured = newValue);

        monitor.Set("999");

        Assert.Equal(999, captured);
    }

    [Fact]
    public void ShouldThrow_WhenTransformerReturnsNull()
    {
        var monitor = new StubOptionsMonitor<string>("bad");

        var wrapper = new TransformingOptionsMonitor<string, object>(monitor, _ => null!);

        Assert.Throws<InvalidOperationException>(() => wrapper.CurrentValue);
    }

    [Fact]
    public void ShouldThrow_WhenSourceValueIsNull()
    {
        var monitor = new StubOptionsMonitor<string?>(null);

        var wrapper = new TransformingOptionsMonitor<string?, int>(monitor, value =>
        {
            if (value is null)
                throw new InvalidOperationException("Cannot transform null value.");
            return int.Parse(value);
        });

        Assert.Throws<InvalidOperationException>(() => wrapper.CurrentValue);
    }


    private class StubOptionsMonitor<T> : IOptionsMonitor<T>
    {
        private readonly List<Action<T, string>> _listeners = [];

        public StubOptionsMonitor(T initial)
        {
            CurrentValue = initial;
        }

        public T CurrentValue { get; private set; }

        public T Get(string? name)
        {
            return CurrentValue;
        }

        public IDisposable OnChange(Action<T, string> listener)
        {
            _listeners.Add(listener);
            return new ListenerUnsubscriber<T>(_listeners, listener);
        }

        public void Set(T newValue, string name = "")
        {
            CurrentValue = newValue;
            foreach (var l in _listeners.ToList())
                l(CurrentValue, name);
        }

        private sealed class ListenerUnsubscriber<TValue>(
            List<Action<TValue, string>> listeners,
            Action<TValue, string> toRemove) : IDisposable
        {
            public void Dispose()
            {
                listeners.Remove(toRemove);
            }
        }
    }
}