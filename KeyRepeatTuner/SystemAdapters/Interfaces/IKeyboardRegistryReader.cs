namespace KeyRepeatTuner.SystemAdapters.Interfaces;

public interface IKeyboardRegistryReader
{
    string? GetRepeatSpeed();
    string? GetRepeatDelay();
}