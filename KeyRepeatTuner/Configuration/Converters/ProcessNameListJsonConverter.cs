using System.Text.Json;
using System.Text.Json.Serialization;
using KeyRepeatTuner.Configuration.ValueObjects;

namespace KeyRepeatTuner.Configuration.Converters;

public class ProcessNameListJsonConverter : JsonConverter<List<ProcessName>>
{
    public override List<ProcessName> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var list = JsonSerializer.Deserialize<List<string>>(ref reader, options);
        return list?.Select(name => new ProcessName(name)).ToList()
               ?? throw new JsonException("ProcessNames list is invalid.");
    }

    public override void Write(Utf8JsonWriter writer, List<ProcessName> value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value.Select(p => p.Value).ToList(), options);
    }
}