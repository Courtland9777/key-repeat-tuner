namespace StarCraftKeyManager.Adapters;

public interface IKeyboardSettingsApplier
{
    void ApplyRepeatSettings(int repeatSpeed, int repeatDelay);
}