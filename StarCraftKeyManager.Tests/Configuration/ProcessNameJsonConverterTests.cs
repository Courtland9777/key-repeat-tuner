using System.Text.Json;
using StarCraftKeyManager.Configuration;
using StarCraftKeyManager.Configuration.Converters;
using StarCraftKeyManager.Configuration.ValueObjects;
using Xunit;

namespace StarCraftKeyManager.Tests.Configuration;

public class ProcessNameJsonConverterTests
{
    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions();
        options.Converters.Add(new ProcessNameJsonConverter());
        return options;
    }

    [Fact]
    public void Deserialize_ValidProcessName_ShouldReturnExpectedValue()
    {
        const string json =
            "{\"ProcessName\":\"validName\",\"KeyRepeat\":{\"Default\":{\"RepeatSpeed\":20,\"RepeatDelay\":500},\"FastMode\":{\"RepeatSpeed\":30,\"RepeatDelay\":250}}}";

        var result = JsonSerializer.Deserialize<AppSettings>(json, CreateOptions());

        Assert.NotNull(result);
        Assert.Equal("validName", result!.ProcessName.Value);
    }

    [Fact]
    public void Deserialize_InvalidProcessName_ShouldThrowJsonException()
    {
        const string json =
            "{\"ProcessName\":\"invalid name.exe\",\"KeyRepeat\":{\"Default\":{\"RepeatSpeed\":20,\"RepeatDelay\":500},\"FastMode\":{\"RepeatSpeed\":30,\"RepeatDelay\":250}}}";

        var ex = Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<AppSettings>(json, CreateOptions()));

        Assert.Contains("Invalid ProcessName format", ex.Message);
    }

    [Fact]
    public void Serialize_ProcessName_ShouldOutputRawString()
    {
        var settings = new AppSettings
        {
            ProcessName = new ProcessName("game123"),
            KeyRepeat = new KeyRepeatSettings
            {
                Default = new KeyRepeatState { RepeatSpeed = 10, RepeatDelay = 750 },
                FastMode = new KeyRepeatState { RepeatSpeed = 20, RepeatDelay = 500 }
            }
        };

        var json = JsonSerializer.Serialize(settings, CreateOptions());

        Assert.Contains("\"ProcessName\":\"game123\"", json);
    }
}