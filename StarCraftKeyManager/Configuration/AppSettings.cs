using System.Text.Json.Serialization;
using StarCraftKeyManager.Configuration.Converters;
using StarCraftKeyManager.Configuration.ValueObjects;

namespace StarCraftKeyManager.Configuration;

public class AppSettings
{
    [JsonConverter(typeof(ProcessNameListJsonConverter))]
    public required List<ProcessName> ProcessNames { get; set; }

    public required KeyRepeatSettings KeyRepeat { get; set; }
}