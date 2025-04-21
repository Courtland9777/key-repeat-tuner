using KeyRepeatTuner.SystemAdapters.Interfaces;
using KeyRepeatTuner.SystemAdapters.User;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace KeyRepeatTuner.Tests.SystemAdapters.User;

public class AppStartupGuardsTests
{
    private readonly Mock<ILoggerFactory> _loggerFactoryMock = new();
    private readonly Mock<ILogger> _loggerMock = new();
    private readonly ServiceProvider _serviceProvider;
    private readonly Mock<IUserContext> _userContextMock = new();

    public AppStartupGuardsTests()
    {
        _loggerFactoryMock.Setup(f => f.CreateLogger(It.IsAny<string>())).Returns(_loggerMock.Object);

        var services = new ServiceCollection();
        services.AddSingleton(_userContextMock.Object);
        services.AddSingleton(_loggerFactoryMock.Object);

        _serviceProvider = services.BuildServiceProvider();
    }

    [Fact]
    public void Should_ReturnTrue_WhenUserIsAdmin()
    {
        _userContextMock.Setup(u => u.IsAdministrator()).Returns(true);

        var result = AppStartupGuards.ValidateAdministratorPrivileges(_serviceProvider);

        Assert.True(result);
    }

    [Fact]
    public void Should_ReturnTrue_WhenSkipAdminEnvIsSet()
    {
        Environment.SetEnvironmentVariable("SKIP_ADMIN_CHECK", "true");

        var result = AppStartupGuards.ValidateAdministratorPrivileges(_serviceProvider);

        Environment.SetEnvironmentVariable("SKIP_ADMIN_CHECK", null);

        Assert.True(result);
    }
}