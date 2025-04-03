using Microsoft.Extensions.Logging;
using Moq;

namespace KeyRepeatTuner.Tests.TestUtilities.Extensions;

public static class LoggerExtensions
{
    public static void VerifyLogContains<T>(
        this Mock<ILogger<T>> loggerMock,
        LogLevel level,
        string messagePart,
        Times? times = null)
    {
        times ??= Times.AtLeastOnce();

        loggerMock.Verify(
            x => x.Log(
                level,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains(messagePart)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            times.Value
        );
    }

    public static void VerifyLogDoesNotContain<T>(
        this Mock<ILogger<T>> loggerMock,
        string messagePart)
    {
        loggerMock.Verify(
            x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains(messagePart)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never()
        );
    }
}