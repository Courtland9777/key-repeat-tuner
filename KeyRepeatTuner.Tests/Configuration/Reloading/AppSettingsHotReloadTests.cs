﻿using KeyRepeatTuner.Configuration;
using KeyRepeatTuner.Configuration.ValueObjects;
using KeyRepeatTuner.Core.Interfaces;
using KeyRepeatTuner.Monitoring.Interfaces;
using KeyRepeatTuner.Monitoring.Services;
using KeyRepeatTuner.Tests.TestUtilities.Stubs;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace KeyRepeatTuner.Tests.Configuration.Reloading;

public class AppSettingsHotReloadTests
{
    [Fact]
    public void RuntimeSettingsChange_ShouldTriggerProcessWatcherAndRouter()
    {
        // Arrange
        var initial = new AppSettings
        {
            ProcessNames = [new ProcessName("notepad")],
            KeyRepeat = new KeyRepeatSettings
            {
                Default = new KeyRepeatState { RepeatSpeed = 20, RepeatDelay = 750 },
                FastMode = new KeyRepeatState { RepeatSpeed = 25, RepeatDelay = 500 }
            }
        };

        var updated = new AppSettings
        {
            ProcessNames = [new ProcessName("starcraft")],
            KeyRepeat = new KeyRepeatSettings
            {
                Default = new KeyRepeatState { RepeatSpeed = 10, RepeatDelay = 1000 },
                FastMode = new KeyRepeatState { RepeatSpeed = 30, RepeatDelay = 300 }
            }
        };

        var mockOptionsMonitor = new TestOptionsMonitor<AppSettings>(initial);
        var mockWatcher = new Mock<IProcessEventWatcher>();
        var mockRouter = new Mock<IProcessEventRouter>();
        var mockKeyRepeatSettingsService = new Mock<IKeyRepeatSettingsService>();
        var mockSettingsChangeHandler = new Mock<IAppSettingsChangeHandler>();
        var mockLogger = new Mock<ILogger<StartupWatcherTrigger>>();

        var trigger = new StartupWatcherTrigger(mockOptionsMonitor, mockWatcher.Object, mockRouter.Object,
            mockKeyRepeatSettingsService.Object, mockSettingsChangeHandler.Object, mockLogger.Object);

        // Act
        trigger.Trigger(); // sets up OnChange listener and triggers once
        mockOptionsMonitor.TriggerChange(updated); // simulates runtime config change

        // Assert
        mockWatcher.Verify(w => w.OnSettingsChanged(initial), Times.Once); // initial
        mockWatcher.Verify(w => w.OnSettingsChanged(updated), Times.Once); // after change
    }
}