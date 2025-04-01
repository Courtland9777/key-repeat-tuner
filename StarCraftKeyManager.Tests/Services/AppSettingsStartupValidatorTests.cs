using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using StarCraftKeyManager.Configuration;
using StarCraftKeyManager.Services;
using StarCraftKeyManager.Tests.Configuration;
using Xunit;

namespace StarCraftKeyManager.Tests.Services;

public class AppSettingsStartupValidatorTests
{
    [Fact]
    public async Task StartAsync_ValidSettings_ShouldLogAndComplete()
    {
        var validSettings = AppSettingsFactory.CreateDefault();

        var mockLogger = new Mock<ILogger<AppSettingsStartupValidator>>();
        var mockOptions = Mock.Of<IOptions<AppSettings>>(o => o.Value == validSettings);

        var mockValidator = new Mock<IValidator<AppSettings>>();
        mockValidator.Setup(v => v.Validate(validSettings))
            .Returns(new ValidationResult());

        var validator = new AppSettingsStartupValidator(mockOptions, mockValidator.Object, mockLogger.Object);

        await validator.StartAsync(CancellationToken.None); // should complete without exception
    }

    [Fact]
    public async Task StartAsync_InvalidSettings_ShouldThrowAndLog()
    {
        var invalidSettings = new AppSettings
        {
            ProcessName = "starcraft",
            KeyRepeat = new KeyRepeatSettings
            {
                Default = null!,
                FastMode = new KeyRepeatState { RepeatSpeed = 20, RepeatDelay = 500 }
            }
        };

        var mockLogger = new Mock<ILogger<AppSettingsStartupValidator>>();
        var mockOptions = Mock.Of<IOptions<AppSettings>>(o => o.Value == invalidSettings);

        var mockValidator = new Mock<IValidator<AppSettings>>();
        mockValidator.Setup(v => v.Validate(It.IsAny<AppSettings>()))
            .Returns(new ValidationResult([
                new ValidationFailure("KeyRepeat.Default", "Missing Default settings")
            ]));

        var validator = new AppSettingsStartupValidator(mockOptions, mockValidator.Object, mockLogger.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(() => validator.StartAsync(CancellationToken.None));
    }
}