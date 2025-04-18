using KeyRepeatTuner.Configuration.ValueObjects;
using Xunit;

namespace KeyRepeatTuner.Tests.Configuration;

public class ProcessNameTests
{
    [Theory]
    [InlineData("starcraft")]
    [InlineData("game_1")]
    [InlineData("DOSBOX")]
    [InlineData("war-craft")]
    public void Constructor_ValidNames_ShouldNormalizeAndStore(string input)
    {
        var name = new ProcessName(input);
        Assert.Equal(input.ToLowerInvariant(), name.Value.ToLowerInvariant());
    }

    [Theory]
    [InlineData("invalid name")]
    [InlineData("bad$name")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("file!.exe")]
    public void Constructor_InvalidNames_ShouldThrow(string input)
    {
        Assert.Throws<ArgumentException>(() => new ProcessName(input));
    }


    [Fact]
    public void WithExe_ShouldAppendExeExtension()
    {
        var pn = new ProcessName("quake");
        Assert.Equal("quake.exe", pn.WithExe());
    }
}