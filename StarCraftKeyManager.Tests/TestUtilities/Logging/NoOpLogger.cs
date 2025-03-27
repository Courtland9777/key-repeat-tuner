using Microsoft.Extensions.Logging;

namespace StarCraftKeyManager.Tests.TestUtilities.Logging;

public sealed class NoOpLogger<T> : ILogger<T>
{
    IDisposable ILogger.BeginScope<TState>(TState state)
    {
        return NullScope.Instance;
    }

    bool ILogger.IsEnabled(LogLevel logLevel)
    {
        return false;
    }

    void ILogger.Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        // No-op
    }

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();

        public void Dispose()
        {
        }
    }
}