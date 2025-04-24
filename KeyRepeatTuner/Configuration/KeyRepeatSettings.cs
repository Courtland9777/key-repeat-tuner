namespace KeyRepeatTuner.Configuration;

public class KeyRepeatSettings
{
    public required KeyRepeatState Default { get; set; }

    public required KeyRepeatState FastMode { get; set; }
}