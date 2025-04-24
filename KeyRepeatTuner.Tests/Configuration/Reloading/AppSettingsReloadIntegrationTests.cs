using KeyRepeatTuner.Configuration;
using KeyRepeatTuner.Configuration.Dto;
using KeyRepeatTuner.Configuration.Validation;
using KeyRepeatTuner.Configuration.ValueObjects;
using KeyRepeatTuner.Core.Interfaces;
using KeyRepeatTuner.Tests.TestUtilities.Stubs;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace KeyRepeatTuner.Tests.Configuration.Reloading;

public class AppSettingsReloadIntegrationTests
{
    [Fact]
    public void TransformedOptionsMonitor_ShouldCallOnChange_OnMappedAndValidatedAppSettings()
    {
        // Arrange
        var initialDto = new AppSettingsDto
        {
            ProcessNames = ["notepad"],
            KeyRepeat = new KeyRepeatSettings
            {
                Default = new KeyRepeatState { RepeatSpeed = 25, RepeatDelay = 750 },
                FastMode = new KeyRepeatState { RepeatSpeed = 15, RepeatDelay = 500 }
            }
        };

        var updatedDto = new AppSettingsDto
        {
            ProcessNames = ["starcraft"],
            KeyRepeat = new KeyRepeatSettings
            {
                Default = new KeyRepeatState { RepeatSpeed = 10, RepeatDelay = 1000 },
                FastMode = new KeyRepeatState { RepeatSpeed = 5, RepeatDelay = 250 }
            }
        };

        var dtoMonitor = new TestOptionsMonitor<AppSettingsDto>(initialDto);
        var validator = new AppSettingsValidator();

        var mockHandler = new Mock<IAppSettingsChangeHandler>();

        var monitor = new TransformingOptionsMonitor<AppSettingsDto, AppSettings>(
            dtoMonitor,
            dto =>
            {
                var mapped = new AppSettings
                {
                    ProcessNames = dto.ProcessNames?.Select(name => new ProcessName(name)).ToList()
                                   ?? throw new InvalidOperationException("ProcessNames must be set"),
                    KeyRepeat = dto.KeyRepeat ?? throw new InvalidOperationException("KeyRepeat settings missing")
                };

                var result = validator.Validate(mapped);
                if (!result.IsValid)
                    throw new OptionsValidationException(nameof(AppSettings), typeof(AppSettings),
                        result.Errors.Select(e => e.ErrorMessage));

                return mapped;
            });

        monitor.OnChange((appSettings, name) => { mockHandler.Object.OnSettingsChanged(appSettings); });

        // Act
        dtoMonitor.TriggerChange(updatedDto);

        // Assert
        mockHandler.Verify(h => h.OnSettingsChanged(It.Is<AppSettings>(s => s.ProcessNames.Count == 1 &&
                                                                            s.ProcessNames[0].Value == "starcraft" &&
                                                                            s.KeyRepeat.Default.RepeatSpeed == 10 &&
                                                                            s.KeyRepeat.FastMode.RepeatDelay == 250)),
            Times.Once);
    }

    [Fact]
    public void InvalidSettings_ShouldThrowValidationError_AndNotCallChangeHandler()
    {
        // Arrange
        var initialDto = new AppSettingsDto
        {
            ProcessNames = ["notepad"],
            KeyRepeat = new KeyRepeatSettings
            {
                Default = new KeyRepeatState { RepeatSpeed = 25, RepeatDelay = 750 },
                FastMode = new KeyRepeatState { RepeatSpeed = 15, RepeatDelay = 500 }
            }
        };

        var invalidDto = new AppSettingsDto
        {
            ProcessNames = ["bad*name"], // fails regex
            KeyRepeat = new KeyRepeatSettings
            {
                Default = new KeyRepeatState { RepeatSpeed = 99, RepeatDelay = 100 }, // invalid range
                FastMode = new KeyRepeatState { RepeatSpeed = -1, RepeatDelay = 10000 } // invalid range
            }
        };

        var dtoMonitor = new TestOptionsMonitor<AppSettingsDto>(initialDto);
        var validator = new AppSettingsValidator();
        var mockHandler = new Mock<IAppSettingsChangeHandler>();

        var monitor = new TransformingOptionsMonitor<AppSettingsDto, AppSettings>(
            dtoMonitor,
            dto =>
            {
                var mapped = new AppSettings
                {
                    ProcessNames = dto.ProcessNames?.Select(name => new ProcessName(name)).ToList()
                                   ?? throw new InvalidOperationException("ProcessNames must be set"),
                    KeyRepeat = dto.KeyRepeat ?? throw new InvalidOperationException("KeyRepeat settings missing")
                };

                var result = validator.Validate(mapped);
                if (!result.IsValid)
                    throw new OptionsValidationException(nameof(AppSettings), typeof(AppSettings),
                        result.Errors.Select(e => e.ErrorMessage));

                return mapped;
            });

        monitor.OnChange((settings, name) => { mockHandler.Object.OnSettingsChanged(settings); });

        // Act + Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            dtoMonitor.TriggerChange(invalidDto));

        Assert.Contains("Invalid process name format", ex.Message);

        mockHandler.Verify(h => h.OnSettingsChanged(It.IsAny<AppSettings>()), Times.Never);
    }
}