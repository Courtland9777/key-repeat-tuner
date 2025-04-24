using KeyRepeatTuner.Configuration.ValueObjects;

namespace KeyRepeatTuner.Tests.TestUtilities.Extensions;

public static class BuilderExtensions
{
    internal static ProcessName CreateProcessName(string process)
    {
        return new ProcessName(process);
    }
}