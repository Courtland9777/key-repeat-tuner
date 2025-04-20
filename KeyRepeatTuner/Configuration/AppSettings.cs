using System.Diagnostics.CodeAnalysis;
using KeyRepeatTuner.Configuration.ValueObjects;

namespace KeyRepeatTuner.Configuration;

// ReSharper disable once PartialTypeWithSinglePart
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
public partial class AppSettings
{
    public AppSettings()
    {
        _ = 0;
    }

    public required List<ProcessName> ProcessNames { get; init; }

    public required KeyRepeatSettings KeyRepeat { get; init; }
}