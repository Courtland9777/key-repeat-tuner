using System.Text.Json.Serialization;
using KeyRepeatTuner.Configuration.Converters;
using KeyRepeatTuner.Configuration.ValueObjects;

namespace KeyRepeatTuner.Configuration;

public class AppSettings
{
    [JsonConverter(typeof(ProcessNameListJsonConverter))]
    public required List<ProcessName> ProcessNames { get; set; }

    public required KeyRepeatSettings KeyRepeat { get; set; }
}