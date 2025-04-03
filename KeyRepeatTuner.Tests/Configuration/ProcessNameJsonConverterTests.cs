using System.Text.Json;
using KeyRepeatTuner.Configuration;
using KeyRepeatTuner.Configuration.Converters;
using KeyRepeatTuner.Configuration.ValueObjects;
using Xunit;

namespace KeyRepeatTuner.Tests.Configuration;

public class ProcessNameJsonConverterTests
{
    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions();
        options.Converters.Add(new ProcessNameJsonConverter());
        return options;
    }

    [Fact]
    public void Deserialize_ValidProcessNames_ShouldReturnExpectedValue()
    {
        const string json =
            "{\"ProcessNames\":[\"validName1\",\"validName2\"],\"KeyRepeat\":{\"Default\":{\"RepeatSpeed\":20,\"RepeatDelay\":500},\"FastMode\":{\"RepeatSpeed\":30,\"RepeatDelay\":250}}}";

        var result = JsonSerializer.Deserialize<AppSettings>(json, CreateOptions());

        Assert.NotNull(result);
        Assert.Collection(result!.ProcessNames,
            pn => Assert.Equal("validName1", pn.Value),
            pn => Assert.Equal("validName2", pn.Value));
    }


    [Fact]
    public void Deserialize_InvalidProcessName_ShouldThrowJsonException()
    {
        // Arrange
        var json = "\"invalid name with spaces\"";

        var options = new JsonSerializerOptions
        {
            Converters = { new ProcessNameJsonConverter() }
        };

        // Act
        var ex = Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<ProcessName>(json, options));

        // Assert
        Assert.NotNull(ex.InnerException);
        Assert.Contains("Invalid process name format", ex.InnerException!.Message);
    }


    [Fact]
    public void Serialize_ProcessName_ShouldOutputRawString()
    {
        // Arrange
        var appSettings = new AppSettings
        {
            ProcessNames = [new ProcessName("game123")],
            KeyRepeat = new KeyRepeatSettings
            {
                Default = new KeyRepeatState { RepeatSpeed = 20, RepeatDelay = 500 },
                FastMode = new KeyRepeatState { RepeatSpeed = 31, RepeatDelay = 250 }
            }
        };

        var json = JsonSerializer.Serialize(appSettings, new JsonSerializerOptions
        {
            Converters = { new ProcessNameListJsonConverter() },
            WriteIndented = false
        });

        // Assert
        Assert.Contains("\"ProcessNames\":[\"game123\"]", json);
    }
}