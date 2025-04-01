using StarCraftKeyManager.Configuration.ValueObjects;
using Xunit;

namespace StarCraftKeyManager.Tests.Configuration;

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
    public void ImplicitConversion_ToString_ShouldReturnRawValue()
    {
        ProcessName pn = new("game");
        string str = pn;
        Assert.Equal("game", str);
    }

    [Fact]
    public void ImplicitConversion_FromString_ShouldCreateInstance()
    {
        const string str = "notepad";
        ProcessName pn = str;
        Assert.Equal("notepad", pn.Value);
    }

    [Fact]
    public void WithExe_ShouldAppendExeExtension()
    {
        var pn = new ProcessName("quake");
        Assert.Equal("quake.exe", pn.WithExe());
    }
}