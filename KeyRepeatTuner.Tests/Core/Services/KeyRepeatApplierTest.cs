using KeyRepeatTuner.Configuration;
using KeyRepeatTuner.Core.Services;
using KeyRepeatTuner.SystemAdapters.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace KeyRepeatTuner.Tests.Core.Services;

public class KeyRepeatApplierTest
{
    private readonly Mock<ILogger<KeyRepeatApplier>> _mockLogger = new();
    private readonly Mock<IKeyboardSettingsApplier> _mockNativeApplier = new();

    private KeyRepeatApplier CreateSut()
    {
        return new KeyRepeatApplier(_mockLogger.Object, _mockNativeApplier.Object);
    }

    [Fact]
    public void Apply_ShouldCallNativeApplier_WithCorrectValues()
    {
        var sut = CreateSut();

        var state = new KeyRepeatState
        {
            RepeatSpeed = 18,
            RepeatDelay = 750
        };

        sut.Apply(state);

        _mockNativeApplier.Verify(a =>
            a.ApplyRepeatSettings(18, 750), Times.Once);
    }

    [Fact]
    public void Apply_ShouldLogError_WhenApplierThrows()
    {
        _mockNativeApplier
            .Setup(a => a.ApplyRepeatSettings(It.IsAny<int>(), It.IsAny<int>()))
            .Throws(new InvalidOperationException("Registry access denied"));

        var sut = CreateSut();

        var state = new KeyRepeatState
        {
            RepeatSpeed = 31,
            RepeatDelay = 500
        };

        sut.Apply(state);

        _mockLogger.Verify(
            log => log.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("Failed to apply")),
                It.Is<Exception>(ex => ex is InvalidOperationException),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}