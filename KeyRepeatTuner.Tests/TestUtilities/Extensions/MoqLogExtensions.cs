using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace KeyRepeatTuner.Tests.TestUtilities.Extensions;

public static class MoqLogExtensions
{
    public static It.IsAnyType MatchLogState(string expectedText)
    {
        return It.Is<It.IsAnyType>((v, _) =>
            v.ToString() != null && v.ToString()!.Contains(expectedText));
    }

    public static Action<LogLevel, EventId, object, Exception?, Func<object, Exception?, string>> MatchesLog(
        string expectedText
    )
    {
        return (level, _, state, _, _) =>
        {
            var message = state?.ToString();
            Assert.NotNull(message);
            Assert.Contains(expectedText, message!);
        };
    }
}