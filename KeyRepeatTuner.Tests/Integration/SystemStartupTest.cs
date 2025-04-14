using KeyRepeatTuner.Infrastructure.ServiceCollection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace KeyRepeatTuner.Tests.Integration;

public class SystemStartupTest
{
    [Fact]
    public async Task Host_ShouldStartSuccessfully()
    {
        // Arrange
        var builder = Host.CreateApplicationBuilder();

        builder.AddValidatedAppSettings();
        builder.AddApplicationServices();

        var host = builder.Build();

        // Act
        var ex = await Record.ExceptionAsync(() => host.StartAsync());

        // Assert
        Assert.Null(ex); // Ensure full DI + options validation succeeds
    }
}