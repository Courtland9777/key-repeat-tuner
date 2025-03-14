using Serilog;
using StarCraftKeyManager.Helpers;

if (!ConfigurationHelpers.IsRunningAsAdmin())
{
    Log.Error("Application is not running as administrator. Please run as administrator.");
    Environment.Exit(1);
}

var builder = Host.CreateApplicationBuilder(args);
builder.SetServiceName();
builder.AddAppSettingsJson();
builder.ConfigureSerilog();
builder.AddApplicationServices();

using var app = builder.Build();

Log.Information("Process Monitor Service Started. Press Ctrl+C to exit.");
await app.RunAsync();