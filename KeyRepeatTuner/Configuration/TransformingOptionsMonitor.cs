using Microsoft.Extensions.Options;

namespace KeyRepeatTuner.Configuration;

public sealed class TransformingOptionsMonitor<TIn, TOut> : IOptionsMonitor<TOut>
    where TOut : notnull
{
    private readonly IOptionsMonitor<TIn> _inner;
    private readonly Func<TIn, TOut> _transform;

    public TransformingOptionsMonitor(IOptionsMonitor<TIn> inner, Func<TIn, TOut> transform)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _transform = transform ?? throw new ArgumentNullException(nameof(transform));
    }

    public TOut CurrentValue => Transform(_inner.CurrentValue);

    public TOut Get(string? name)
    {
        return Transform(_inner.Get(name));
    }

    public IDisposable OnChange(Action<TOut, string> listener)
    {
        ArgumentNullException.ThrowIfNull(listener);

        return _inner.OnChange((dto, name) =>
        {
            var transformed = Transform(dto);
            listener(transformed, name ?? string.Empty);
        })!;
    }

    private TOut Transform(TIn? source)
    {
        if (source is null)
            throw new InvalidOperationException($"Options for {typeof(TIn).Name} are not configured.");

        return _transform(source) ??
               throw new InvalidOperationException($"Transformed {typeof(TOut).Name} cannot be null.");
    }
}