﻿namespace KeyRepeatTuner.SystemAdapters.Interfaces;

public interface IKeyboardSettingsApplier
{
    void ApplyRepeatSettings(int repeatSpeed, int repeatDelay);
}