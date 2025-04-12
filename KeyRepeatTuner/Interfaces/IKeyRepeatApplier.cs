using KeyRepeatTuner.Configuration;

namespace KeyRepeatTuner.Interfaces;

public interface IKeyRepeatApplier
{
    void Apply(KeyRepeatState state);
}