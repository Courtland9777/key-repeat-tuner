using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using StarCraftKeyManager.Configuration;
using StarCraftKeyManager.Configuration.ValueObjects;
using StarCraftKeyManager.Events;
using StarCraftKeyManager.Handlers;
using Xunit;

namespace StarCraftKeyManager.Tests.Handlers;

public class AppSettingsStartupValidationHandlerTests
{
    private readonly Mock<ILogger<AppSettingsStartupValidationHandler>> _mockLogger = new();
    private readonly Mock<IOptions<AppSettings>> _mockOptions = new();
    private readonly Mock<IValidator<AppSettings>> _mockValidator = new();

    [Fact]
    public async Task Handle_ShouldPass_WhenSettingsAreValid()
    {
        var validSettings = new AppSettings
        {
            ProcessNames = [new ProcessName("starcraft"), new ProcessName("notepad")],
            KeyRepeat = new KeyRepeatSettings
            {
                Default = new KeyRepeatState { RepeatSpeed = 20, RepeatDelay = 750 },
                FastMode = new KeyRepeatState { RepeatSpeed = 10, RepeatDelay = 500 }
            }
        };

        _mockOptions.Setup(o => o.Value).Returns(validSettings);
        _mockValidator.Setup(v => v.Validate(validSettings)).Returns(new ValidationResult());

        var handler = new AppSettingsStartupValidationHandler(
            _mockOptions.Object, _mockValidator.Object, _mockLogger.Object);

        var exception = await Record.ExceptionAsync(() =>
            handler.Handle(new AppStartupInitiated(), default));

        Assert.Null(exception); // should not throw
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenSettingsAreInvalid()
    {
        var invalidSettings = new AppSettings
        {
            ProcessNames = [new ProcessName("starcraft"), new ProcessName("notepad")],
            KeyRepeat = new KeyRepeatSettings
            {
                Default = null!,
                FastMode = null!
            }
        };

        _mockOptions.Setup(o => o.Value).Returns(invalidSettings);
        _mockValidator.Setup(v => v.Validate(It.IsAny<AppSettings>()))
            .Returns(new ValidationResult([
                new ValidationFailure("KeyRepeat.Default", "Default missing"),
                new ValidationFailure("KeyRepeat.FastMode", "FastMode missing")
            ]));

        var handler = new AppSettingsStartupValidationHandler(
            _mockOptions.Object, _mockValidator.Object, _mockLogger.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(new AppStartupInitiated(), default));
    }
}