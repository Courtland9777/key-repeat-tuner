using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using StarCraftKeyManager.Configuration;
using StarCraftKeyManager.Configuration.ValueObjects;
using StarCraftKeyManager.Services;
using StarCraftKeyManager.SystemAdapters.Interfaces;
using Xunit;

namespace StarCraftKeyManager.Tests.Services;

public class KeyRepeatSettingsServiceTests
{
    private readonly Mock<IKeyboardSettingsApplier> _mockKeyboardApplier = new();
    private readonly Mock<ILogger<KeyRepeatSettingsService>> _mockLogger = new();

    private KeyRepeatSettingsService CreateService(AppSettings settings)
    {
        var mockOptions = new Mock<IOptionsMonitor<AppSettings>>();
        mockOptions.Setup(o => o.CurrentValue).Returns(settings);
        return new KeyRepeatSettingsService(_mockLogger.Object, _mockKeyboardApplier.Object, mockOptions.Object);
    }

    [Fact]
    public void UpdateRunningState_ShouldApplyFastMode_WhenRunning()
    {
        var settings = new AppSettings
        {
            ProcessNames = [new ProcessName("sc"), new ProcessName("notepad")],
            KeyRepeat = new KeyRepeatSettings
            {
                Default = new KeyRepeatState { RepeatSpeed = 10, RepeatDelay = 750 },
                FastMode = new KeyRepeatState { RepeatSpeed = 20, RepeatDelay = 500 }
            }
        };

        var service = CreateService(settings);

        service.UpdateRunningState(true);

        _mockKeyboardApplier.Verify(a => a.ApplyRepeatSettings(20, 500), Times.Once);
    }

    [Fact]
    public void UpdateRunningState_ShouldApplyDefaultMode_WhenNotRunning()
    {
        var settings = new AppSettings
        {
            ProcessNames = ["sc"],
            KeyRepeat = new KeyRepeatSettings
            {
                Default = new KeyRepeatState { RepeatSpeed = 10, RepeatDelay = 750 },
                FastMode = new KeyRepeatState { RepeatSpeed = 20, RepeatDelay = 500 }
            }
        };

        var service = CreateService(settings);

        service.UpdateRunningState(false);

        _mockKeyboardApplier.Verify(a => a.ApplyRepeatSettings(10, 750), Times.Once);
    }

    [Fact]
    public void OnSettingsChanged_ShouldUpdateInternalState()
    {
        var initial = new AppSettings
        {
            ProcessNames = ["sc"],
            KeyRepeat = new KeyRepeatSettings
            {
                Default = new KeyRepeatState { RepeatSpeed = 10, RepeatDelay = 750 },
                FastMode = new KeyRepeatState { RepeatSpeed = 20, RepeatDelay = 500 }
            }
        };

        var updated = new AppSettings
        {
            ProcessNames = ["sc"],
            KeyRepeat = new KeyRepeatSettings
            {
                Default = new KeyRepeatState { RepeatSpeed = 5, RepeatDelay = 250 },
                FastMode = new KeyRepeatState { RepeatSpeed = 15, RepeatDelay = 400 }
            }
        };

        var service = CreateService(initial);

        service.OnSettingsChanged(updated);
        service.UpdateRunningState(false); // should now use updated.Default

        _mockKeyboardApplier.Verify(a => a.ApplyRepeatSettings(5, 250), Times.Once);
    }

    [Fact]
    public void UpdateRunningState_ShouldNotThrow_WhenApplierFails()
    {
        _mockKeyboardApplier.Setup(a => a.ApplyRepeatSettings(It.IsAny<int>(), It.IsAny<int>()))
            .Throws<InvalidOperationException>();

        var settings = new AppSettings
        {
            ProcessNames = ["sc"],
            KeyRepeat = new KeyRepeatSettings
            {
                Default = new KeyRepeatState { RepeatSpeed = 1, RepeatDelay = 500 },
                FastMode = new KeyRepeatState { RepeatSpeed = 1, RepeatDelay = 500 }
            }
        };

        var service = CreateService(settings);

        var exception = Record.Exception(() => service.UpdateRunningState(true));
        Assert.Null(exception); // should log, but not throw
    }
}