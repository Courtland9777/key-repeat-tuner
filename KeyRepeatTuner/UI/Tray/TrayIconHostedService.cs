using System.Diagnostics;
using System.Reflection;
using KeyRepeatTuner.Configuration;
using Microsoft.Extensions.Options;

namespace KeyRepeatTuner.UI.Tray;

public sealed class TrayIconHostedService : IHostedService
{
    private readonly IHostApplicationLifetime _lifetime;
    private readonly IOptionsMonitor<AppSettings> _settingsMonitor;
    private NotifyIcon? _trayIcon;

    public TrayIconHostedService(
        IHostApplicationLifetime lifetime,
        IOptionsMonitor<AppSettings> settingsMonitor)
    {
        _lifetime = lifetime;
        _settingsMonitor = settingsMonitor;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        Application.EnableVisualStyles();

        _trayIcon = new NotifyIcon
        {
            Icon = SystemIcons.Application,
            Visible = true,
            Text = "KeyRepeatTuner",
            ContextMenuStrip = BuildContextMenu()
        };

        _settingsMonitor.OnChange(_ => { _trayIcon.Text = $"KeyRepeatTuner - Watching {GetProcessListTooltip()}"; });

        _trayIcon.Text = $"KeyRepeatTuner - Watching {GetProcessListTooltip()}";

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        if (_trayIcon != null)
        {
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
            _trayIcon = null;
        }

        return Task.CompletedTask;
    }

    private ContextMenuStrip BuildContextMenu()
    {
        var menu = new ContextMenuStrip();

        var openSettings = new ToolStripMenuItem("Open Settings", null, (_, _) => OpenAppSettings());
        var exitItem = new ToolStripMenuItem("Exit", null, (_, _) => _lifetime.StopApplication());

        menu.Items.Add(openSettings);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(exitItem);

        return menu;
    }

    private static void OpenAppSettings()
    {
        var exeDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        var settingsPath = Path.Combine(exeDir, "appsettings.json");

        if (!File.Exists(settingsPath))
        {
            MessageBox.Show("appsettings.json not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = settingsPath,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not open settings: {ex.Message}", "Error", MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private string GetProcessListTooltip()
    {
        var names = _settingsMonitor.CurrentValue.ProcessNames?.Select(p => p.Value) ?? [];
        return string.Join(", ", names);
    }
}