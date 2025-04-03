using System.Text.Json;
using System.Text.Json.Serialization;
using KeyRepeatTuner.Configuration.ValueObjects;

namespace KeyRepeatTuner.Configuration.Converters;

public class ProcessNameListJsonConverter : JsonConverter<List<ProcessName>>
{
    public override List<ProcessName> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var list = JsonSerializer.Deserialize<List<string>>(ref reader, options);

        if (list is null)
            throw new JsonException("ProcessNames list is invalid.");

        try
        {
            return list.Select(name => new ProcessName(name)).ToList();
        }
        catch (ArgumentException ex)
        {
            throw new JsonException("Invalid ProcessName format", ex);
        }
    }


    public override void Write(Utf8JsonWriter writer, List<ProcessName> value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value.Select(p => p.Value).ToList(), options);
    }
}