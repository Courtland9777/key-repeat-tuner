using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Options;
using StarCraftKeyManager.Interfaces;
using StarCraftKeyManager.Interop;
using StarCraftKeyManager.Models;

namespace StarCraftKeyManager.Services;

internal class ProcessMonitorService : BackgroundService, IProcessMonitorService
{
    private readonly ILogger<ProcessMonitorService> _logger;
    private EventLogWatcher? _eventWatcher;
    private bool _isRunning;
    private KeyRepeatSettings _keyRepeatSettings;
    private int _processCount;
    private string _processName;

    public ProcessMonitorService(ILogger<ProcessMonitorService> logger, IOptionsMonitor<AppSettings> optionsMonitor)
    {
        _logger = logger;
        _processName = optionsMonitor.CurrentValue.ProcessMonitor.ProcessName;
        _keyRepeatSettings = optionsMonitor.CurrentValue.KeyRepeat;

        // Monitor settings changes
        optionsMonitor.OnChange(_ =>
        {
            try
            {
                // Trigger validation by accessing CurrentValue
                var validatedSettings = optionsMonitor.CurrentValue;
                _processName = validatedSettings.ProcessMonitor.ProcessName;
                _keyRepeatSettings = validatedSettings.KeyRepeat;
                _logger.LogInformation("Configuration changed. Applying new settings...");
                ApplyKeyRepeatSettings();
            }
            catch (OptionsValidationException ex)
            {
                // Log validation errors
                _logger.LogError("Settings validation failed: {Errors}", string.Join(", ", ex.Failures));
            }
        });
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting Process Monitor for {_processName}", _processName);

        // Get initial process state and apply settings
        UpdateProcessState();
        ApplyKeyRepeatSettings();

        // Subscribe to Windows Event Log for process events
        SubscribeToProcessEvents();

        try
        {
            while (!stoppingToken.IsCancellationRequested) await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Process monitoring stopped.");
            _eventWatcher?.Dispose();
            throw;
        }
        finally
        {
            _eventWatcher?.Dispose();
        }
    }

    private void SubscribeToProcessEvents()
    {
        const string query = @"
        <QueryList>
            <Query Id='0' Path='Security'>
                <Select Path='Security'>
                    *[System[(EventID=4688 or EventID=4689)]]
                </Select>
            </Query>
        </QueryList>";

        var eventQuery = new EventLogQuery("Security", PathType.LogName, query)
        {
            ReverseDirection = true
        };

        _eventWatcher = new EventLogWatcher(eventQuery);
        _eventWatcher.EventRecordWritten += (sender, e) =>
        {
            try
            {
                if (e.EventRecord == null) return;

                var eventId = e.EventRecord.Id;
                var eventProps = e.EventRecord.Properties;

                if (eventProps == null || eventProps.Count == 0) return;

                var processPath = eventId switch
                {
                    4688 when eventProps.Count > 5 => eventProps[5].Value as string,
                    4689 when eventProps.Count > 6 => eventProps[6].Value as string,
                    _ => null
                };

                if (string.IsNullOrEmpty(processPath)) return;

                // Extract process name (remove full path)
                var detectedProcessName = Path.GetFileNameWithoutExtension(processPath);

                if (string.Equals(detectedProcessName, _processName, StringComparison.OrdinalIgnoreCase))
                    UpdateProcessState();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing event log entry.");
            }
        };

        _eventWatcher.Enabled = true;
        _logger.LogInformation("Subscribed to process start/stop events for {_processName}", _processName);
    }


    private void UpdateProcessState()
    {
        var sanitizedProcessName = _processName.Replace(".exe", string.Empty, StringComparison.OrdinalIgnoreCase);
        _processCount = Process.GetProcessesByName(sanitizedProcessName).Length;
        _isRunning = _processCount > 0;
        _logger.LogInformation(
            "[Update] Process Running: {IsRunning}, Count: {ProcessCount}",
            _isRunning,
            _processCount
        );
        ApplyKeyRepeatSettings();
    }

    private void ApplyKeyRepeatSettings()
    {
        var settings = _isRunning ? _keyRepeatSettings.FastMode : _keyRepeatSettings.Default;

        _logger.LogInformation("Applying Key Repeat Settings: RepeatSpeed={RepeatSpeed}, RepeatDelay={RepeatDelay}",
            settings.RepeatSpeed, settings.RepeatDelay);

        try
        {
            SetKeyboardRepeat(settings.RepeatSpeed, settings.RepeatDelay);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update key repeat settings.");
            throw;
        }
    }

    private static void SetKeyboardRepeat(int repeatSpeed, int repeatDelay)
    {
        const uint SPI_SETKEYBOARDSPEED = 0x000B;
        const uint SPI_SETKEYBOARDDELAY = 0x0017;

        if (!NativeMethods.SystemParametersInfo(SPI_SETKEYBOARDSPEED, (uint)repeatSpeed, 0, 0))
            throw new Win32Exception(Marshal.GetLastWin32Error());

        if (!NativeMethods.SystemParametersInfo(SPI_SETKEYBOARDDELAY, (uint)repeatDelay / 250, 0, 0))
            throw new Win32Exception(Marshal.GetLastWin32Error());
    }
}