using System.Reflection;
using System.Security.Principal;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using StarCraftKeyManager.Helpers;
using StarCraftKeyManager.Interfaces;
using StarCraftKeyManager.Models;
using StarCraftKeyManager.Services;
using Xunit;

namespace StarCraftKeyManager.Tests;

public class ConfigurationHelpersTests
{
    private readonly Mock<IHostApplicationBuilder> _mockBuilder;
    private readonly Mock<IConfiguration> _mockConfiguration;

    public ConfigurationHelpersTests()
    {
        _mockBuilder = new Mock<IHostApplicationBuilder>();
        _mockConfiguration = new Mock<IConfiguration>();

        _mockBuilder.Setup(b => b.Services).Returns(new ServiceCollection());
        _mockBuilder.Setup(b => b.Configuration).Returns((IConfigurationManager)_mockConfiguration.Object);
    }

    [Fact]
    public void AddAppSettingsJson_ShouldRegisterAppSettings()
    {
        // Arrange
        var configurationSection = new Mock<IConfigurationSection>();
        _mockConfiguration.Setup(c => c.GetSection("AppSettings")).Returns(configurationSection.Object);

        // Act
        _mockBuilder.Object.AddAppSettingsJson();

        // Assert
        _mockBuilder.Verify(b => b.Services.Configure<AppSettings>(_mockConfiguration.Object.GetSection("AppSettings")),
            Times.Once);
    }

    [Fact]
    public void SetServiceName_ShouldConfigureWindowsServiceOptions()
    {
        // Act
        _mockBuilder.Object.SetServiceName();

        // Assert
        _mockBuilder.Verify(b => b.Services.Configure(
            It.IsAny<Action<WindowsServiceLifetimeOptions>>()), Times.Once);
    }

    [Fact]
    public void ConfigureSerilog_ShouldSetupLoggingCorrectly()
    {
        // Act
        _mockBuilder.Object.ConfigureSerilog();

        // Assert
        _mockBuilder.Verify(b => b.Logging.ClearProviders(), Times.Once);
    }

    [Fact]
    public void AddApplicationServices_ShouldValidateAppSettings_Success()
    {
        // Arrange
        var serviceCollection = new ServiceCollection();
        var mockValidator = new Mock<IValidator<AppSettings>>();
        mockValidator.Setup(v => v.Validate(It.IsAny<AppSettings>())).Returns(new ValidationResult());

        _mockBuilder.Setup(b => b.Services).Returns(serviceCollection);
        serviceCollection.AddSingleton(mockValidator.Object);
        serviceCollection.Configure<AppSettings>(_ => { });

        // Act
        _mockBuilder.Object.AddApplicationServices();

        // Assert
        mockValidator.Verify(v => v.Validate(It.IsAny<AppSettings>()), Times.Once);
    }

    [Fact]
    public void AddApplicationServices_ShouldThrowException_WhenValidationFails()
    {
        // Arrange
        var serviceCollection = new ServiceCollection();
        var mockValidator = new Mock<IValidator<AppSettings>>();
        var validationErrors = new ValidationResult(new List<ValidationFailure>
        {
            new("ProcessMonitor.ProcessName", "Process name is required.")
        });

        mockValidator.Setup(v => v.Validate(It.IsAny<AppSettings>())).Returns(validationErrors);
        _mockBuilder.Setup(b => b.Services).Returns(serviceCollection);
        serviceCollection.AddSingleton(mockValidator.Object);
        serviceCollection.Configure<AppSettings>(options => options.ProcessMonitor.ProcessName = "");

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => _mockBuilder.Object.AddApplicationServices());
        Assert.Contains("Invalid AppSettings detected", exception.Message);
    }

    [Fact]
    public void RegisterServices_ShouldConfigureDependencies()
    {
        // Arrange
        var serviceCollection = new ServiceCollection();
        var mockConfiguration = new Mock<IConfiguration>();

        // Act
        var method = typeof(ConfigurationHelpers)
            .GetMethod("RegisterServices", BindingFlags.NonPublic | BindingFlags.Static);
        method?.Invoke(null, [serviceCollection, mockConfiguration.Object]);

        // Assert
        Assert.Contains(serviceCollection, s => s.ServiceType == typeof(ProcessMonitorService));
        Assert.Contains(serviceCollection, s => s.ServiceType == typeof(IProcessEventWatcher));
    }

    [Fact]
    public void IsRunningAsAdmin_ShouldReturnCorrectValue()
    {
        // Arrange
        var identity = new Mock<WindowsIdentity>();
        var principal = new Mock<WindowsPrincipal>(identity.Object);
        identity.Setup(i => i.IsAuthenticated).Returns(true);
        principal.Setup(p => p.IsInRole(WindowsBuiltInRole.Administrator)).Returns(true);

        // Act
        var isAdmin = ConfigurationHelpers.IsRunningAsAdmin();

        // Assert
        Assert.True(isAdmin, "Expected the method to return true for an admin user.");
    }
}