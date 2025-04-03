using FluentValidation;
using FluentValidation.Results;
using KeyRepeatTuner.Configuration;
using KeyRepeatTuner.Interfaces;
using KeyRepeatTuner.Services;
using KeyRepeatTuner.Tests.Configuration;
using KeyRepeatTuner.Tests.TestUtilities.Stubs;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace KeyRepeatTuner.Tests.Services;

public class AppSettingsRuntimeCoordinatorTests
{
    [Fact]
    public void InvalidSettings_ShouldNotTriggerHandlers()
    {
        var settings = AppSettingsFactory.CreateDefault();
        var optionsMonitor = new TestOptionsMonitor<AppSettings>(settings);

        var mockValidator = new Mock<IValidator<AppSettings>>();
        mockValidator.Setup(v => v.Validate(It.IsAny<AppSettings>())).Returns(new ValidationResult(
        [
            new ValidationFailure("KeyRepeat", "Invalid")
        ]));

        var mockHandler = new Mock<IAppSettingsChangeHandler>();
        var mockLogger = new Mock<ILogger<AppSettingsRuntimeCoordinator>>();

        _ = new AppSettingsRuntimeCoordinator(mockValidator.Object, [mockHandler.Object], optionsMonitor,
            mockLogger.Object);

        optionsMonitor.TriggerChange(settings);

        mockHandler.Verify(h => h.OnSettingsChanged(It.IsAny<AppSettings>()), Times.Never);
    }

    [Fact]
    public void ValidSettings_ShouldNotifyHandlers()
    {
        var settings = AppSettingsFactory.CreateDefault();
        var optionsMonitor = new TestOptionsMonitor<AppSettings>(settings);

        var mockValidator = new Mock<IValidator<AppSettings>>();
        mockValidator.Setup(v => v.Validate(It.IsAny<AppSettings>()))
            .Returns(new ValidationResult());

        var mockHandler = new Mock<IAppSettingsChangeHandler>();
        var mockLogger = new Mock<ILogger<AppSettingsRuntimeCoordinator>>();

        _ = new AppSettingsRuntimeCoordinator(mockValidator.Object, [mockHandler.Object], optionsMonitor,
            mockLogger.Object);

        optionsMonitor.TriggerChange(settings);

        mockHandler.Verify(h => h.OnSettingsChanged(It.Is<AppSettings>(s => s == settings)), Times.Once);
    }
}