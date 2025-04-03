using System.Text.Json;
using System.Text.Json.Serialization;
using KeyRepeatTuner.Configuration.ValueObjects;

namespace KeyRepeatTuner.Configuration.Converters;

public class ProcessNameJsonConverter : JsonConverter<ProcessName>
{
    public override ProcessName Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var raw = reader.GetString();

        if (string.IsNullOrWhiteSpace(raw))
            throw new JsonException("ProcessName cannot be null or whitespace.");

        try
        {
            return new ProcessName(raw);
        }
        catch (ArgumentException ex)
        {
            throw new JsonException("Invalid ProcessName format.", ex);
        }
    }

    public override void Write(Utf8JsonWriter writer, ProcessName value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.Value);
    }
}