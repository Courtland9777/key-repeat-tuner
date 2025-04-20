using System.Diagnostics.CodeAnalysis;

namespace KeyRepeatTuner.Configuration;

// ReSharper disable once PartialTypeWithSinglePart
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
public partial class AppSettingsDto
{
    public AppSettingsDto()
    {
        _ = 0;
    }

    public required List<string> ProcessNames { get; set; }
    public required KeyRepeatSettings KeyRepeat { get; set; }
}