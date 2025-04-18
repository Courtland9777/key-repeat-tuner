using KeyRepeatTuner.Configuration;
using KeyRepeatTuner.Core.Services;
using Xunit;

namespace KeyRepeatTuner.Tests.Services;

public class KeyRepeatModeResolverTests
{
    private readonly KeyRepeatModeResolver _resolver = new();

    [Fact]
    public void GetTargetState_WhenRunning_ReturnsFastMode()
    {
        var settings = new KeyRepeatSettings
        {
            FastMode = new KeyRepeatState { RepeatSpeed = 2, RepeatDelay = 300 },
            Default = new KeyRepeatState { RepeatSpeed = 25, RepeatDelay = 1000 }
        };

        var result = _resolver.GetTargetState(true, settings);

        Assert.Equal(2, result.RepeatSpeed);
        Assert.Equal(300, result.RepeatDelay);
    }

    [Fact]
    public void GetTargetState_WhenNotRunning_ReturnsDefaultMode()
    {
        var settings = new KeyRepeatSettings
        {
            FastMode = new KeyRepeatState { RepeatSpeed = 2, RepeatDelay = 300 },
            Default = new KeyRepeatState { RepeatSpeed = 25, RepeatDelay = 1000 }
        };

        var result = _resolver.GetTargetState(false, settings);

        Assert.Equal(25, result.RepeatSpeed);
        Assert.Equal(1000, result.RepeatDelay);
    }

    [Theory]
    [InlineData(true, "FastMode")]
    [InlineData(false, "Default")]
    public void GetModeName_ReturnsCorrectLabel(bool isRunning, string expected)
    {
        var mode = _resolver.GetModeName(isRunning);
        Assert.Equal(expected, mode);
    }
}