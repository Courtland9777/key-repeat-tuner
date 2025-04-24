using KeyRepeatTuner.SystemAdapters.Keyboard;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace KeyRepeatTuner.Tests.SystemAdapters.Keyboard;

public class KeyboardSettingsApplierTests
{
    [Fact]
    public void ApplyRepeatSettings_DoesNotThrow_OnValidValues()
    {
        // Arrange
        var logger = new Mock<ILogger<KeyboardSettingsApplier>>();
        var applier = new KeyboardSettingsApplier(logger.Object);

        // Act (this will likely succeed unless your OS blocks it)
        var exception = Record.Exception(() => applier.ApplyRepeatSettings(20, 500));

        // Assert
        Assert.Null(exception);
    }
}