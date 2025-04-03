using System.Text.Json.Serialization;

namespace KeyRepeatTuner.Configuration;

public class KeyRepeatState
{
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
    public int RepeatSpeed { get; init; }

    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
    public int RepeatDelay { get; init; }
}