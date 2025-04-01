using System.Text.Json.Serialization;
using StarCraftKeyManager.Configuration.Converters;
using StarCraftKeyManager.Configuration.ValueObjects;

namespace StarCraftKeyManager.Configuration;

public class AppSettings
{
    [JsonConverter(typeof(ProcessNameJsonConverter))]
    public required ProcessName ProcessName { get; set; }

    public required KeyRepeatSettings KeyRepeat { get; set; }
}