using KeyRepeatTuner.Configuration;
using KeyRepeatTuner.Configuration.Mapping;
using Xunit;

namespace KeyRepeatTuner.Tests.Configuration.Mapping;

public class AppSettingsMapperTests
{
    [Fact]
    public void ToDomain_ShouldMapValidDtoCorrectly()
    {
        var dto = new AppSettingsDto
        {
            ProcessNames = ["notepad", "cmd"],
            KeyRepeat = new KeyRepeatSettings
            {
                Default = new KeyRepeatState { RepeatSpeed = 15, RepeatDelay = 750 },
                FastMode = new KeyRepeatState { RepeatSpeed = 31, RepeatDelay = 500 }
            }
        };

        var domain = AppSettingsMapper.ToDomain(dto);

        Assert.NotNull(domain);
        Assert.Equal(2, domain.ProcessNames.Count);
        Assert.Equal("notepad", domain.ProcessNames[0].Value);
        Assert.Equal(15, domain.KeyRepeat.Default.RepeatSpeed);
    }

    [Fact]
    public void ToDomain_ShouldThrow_WhenProcessNamesIsNull()
    {
        var dto = new AppSettingsDto
        {
            ProcessNames = null!,
            KeyRepeat = new KeyRepeatSettings
            {
                Default = new KeyRepeatState { RepeatSpeed = 20, RepeatDelay = 500 },
                FastMode = new KeyRepeatState { RepeatSpeed = 31, RepeatDelay = 250 }
            }
        };

        Assert.Throws<InvalidOperationException>(() => AppSettingsMapper.ToDomain(dto));
    }

    [Fact]
    public void ToDomain_ShouldThrow_WhenProcessNameContainsWhitespace()
    {
        var dto = new AppSettingsDto
        {
            ProcessNames = ["valid", "   "],
            KeyRepeat = new KeyRepeatSettings
            {
                Default = new KeyRepeatState { RepeatSpeed = 20, RepeatDelay = 500 },
                FastMode = new KeyRepeatState { RepeatSpeed = 31, RepeatDelay = 250 }
            }
        };

        Assert.Throws<InvalidOperationException>(() => AppSettingsMapper.ToDomain(dto));
    }

    [Fact]
    public void ToDomain_ShouldThrow_WhenKeyRepeatIsNull()
    {
        var dto = new AppSettingsDto
        {
            ProcessNames = ["notepad"],
            KeyRepeat = null!
        };

        Assert.Throws<InvalidOperationException>(() => AppSettingsMapper.ToDomain(dto));
    }
}