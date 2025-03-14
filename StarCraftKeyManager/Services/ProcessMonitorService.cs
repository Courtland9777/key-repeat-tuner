using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Runtime.InteropServices;
using System.Threading.Channels;
using Microsoft.Extensions.Options;
using StarCraftKeyManager.Interfaces;
using StarCraftKeyManager.Interop;
using StarCraftKeyManager.Models;

namespace StarCraftKeyManager.Services;

internal class ProcessMonitorService : BackgroundService, IProcessMonitorService
{
    private readonly Channel<(int EventId, int ProcessId, string ProcessPath)> _eventChannel;
    private readonly ILogger<ProcessMonitorService> _logger;
    private readonly HashSet<int> _trackedProcesses = [];
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

        // Event queue for asynchronous event handling
        _eventChannel = Channel.CreateUnbounded<(int, int, string)>();

        // Monitor settings changes
        optionsMonitor.OnChange(_ =>
        {
            try
            {
                var validatedSettings = optionsMonitor.CurrentValue;
                _processName = validatedSettings.ProcessMonitor.ProcessName;
                _keyRepeatSettings = validatedSettings.KeyRepeat;
                _logger.LogInformation("Configuration changed. Applying new settings...");
                ApplyKeyRepeatSettings();
            }
            catch (OptionsValidationException ex)
            {
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

        // Start processing events asynchronously
        await ProcessEventsAsync(stoppingToken);
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
        _eventWatcher.EventRecordWritten += async (sender, e) =>
        {
            try
            {
                if (e.EventRecord == null) return;

                var eventId = e.EventRecord.Id;
                var eventProps = e.EventRecord.Properties;

                if (eventProps == null || eventProps.Count == 0) return;

                string? processPath = null;
                int? processId = null;

                switch (eventId)
                {
                    case 4688 when eventProps.Count > 4:
                        processPath = eventProps[5].Value as string;
                        processId = eventProps[4].Value as int?;
                        break;
                    case 4689 when eventProps.Count > 3:
                        processPath = eventProps[6].Value as string;
                        processId = eventProps[3].Value as int?;
                        break;
                }

                if (string.IsNullOrEmpty(processPath) || !processId.HasValue) return;

                var detectedProcessName = Path.GetFileNameWithoutExtension(processPath);

                if (string.Equals(detectedProcessName, _processName, StringComparison.OrdinalIgnoreCase))
                    // Enqueue the event for asynchronous processing
                    await _eventChannel.Writer.WriteAsync((eventId, processId.Value, detectedProcessName));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing event log entry.");
            }
        };

        _eventWatcher.Enabled = true;
        _logger.LogInformation("Subscribed to process start/stop events for {_processName}", _processName);
    }

    private async Task ProcessEventsAsync(CancellationToken stoppingToken)
    {
        await foreach (var (eventId, processId, processName) in _eventChannel.Reader.ReadAllAsync(stoppingToken))
            try
            {
                _logger.LogDebug("Processing event: {EventId}, ProcessId: {ProcessId}, ProcessName: {ProcessName}",
                    eventId, processId, processName);

                var processStarted = eventId == 4688;
                UpdateProcessState(processId, processStarted);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling process event asynchronously.");
            }
    }

    private void UpdateProcessState(int? processId = null, bool processStarted = false)
    {
        var sanitizedProcessName = _processName.Replace(".exe", string.Empty, StringComparison.OrdinalIgnoreCase);

        if (processId.HasValue)
        {
            if (processStarted)
                _trackedProcesses.Add(processId.Value);
            else
                _trackedProcesses.Remove(processId.Value);
        }
        else
        {
            // Full refresh only when necessary
            _trackedProcesses.Clear();
            foreach (var process in Process.GetProcessesByName(sanitizedProcessName)) _trackedProcesses.Add(process.Id);
        }

        _processCount = _trackedProcesses.Count;
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