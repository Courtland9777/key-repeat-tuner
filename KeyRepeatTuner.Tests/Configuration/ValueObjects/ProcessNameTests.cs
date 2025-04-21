using KeyRepeatTuner.Configuration.ValueObjects;
using Xunit;

namespace KeyRepeatTuner.Tests.Configuration.ValueObjects;

public class ProcessNameTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_ShouldThrow_WhenInputIsNullOrWhitespace(string? input)
    {
        var ex = Assert.Throws<ArgumentException>(() => _ = new ProcessName(input!));
        Assert.Contains("cannot be null or whitespace", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("notepad!")]
    [InlineData("has.dot.exe")]
    [InlineData("space in name.exe")]
    [InlineData("~invalidname")]
    public void Constructor_ShouldThrow_WhenInputFailsRegex(string input)
    {
        var ex = Assert.Throws<ArgumentException>(() => _ = new ProcessName(input));
        Assert.Contains("Invalid process name format", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("notepad", "notepad")]
    [InlineData("notepad.exe", "notepad")]
    [InlineData("  StarCraft  ", "StarCraft")]
    [InlineData("StarCraft.exe", "StarCraft")]
    public void Constructor_ShouldNormalizeValidInputs(string input, string expected)
    {
        var name = new ProcessName(input);

        Assert.Equal(expected, name.Value);
        Assert.Equal($"{expected}.exe", name.WithExe());
        Assert.Equal(expected, name.ToString());
    }

    [Fact]
    public void ProcessName_Struct_EqualsAndHashCode_ShouldBeValid()
    {
        var a = new ProcessName("game");
        var b = new ProcessName("game.exe");
        var c = new ProcessName("GAME");

        Assert.Equal(a, b);
        Assert.NotEqual(a, c); // case-sensitive value object

        Assert.Equal(a.GetHashCode(), b.GetHashCode());
        Assert.NotEqual(a.GetHashCode(), c.GetHashCode());
    }
}