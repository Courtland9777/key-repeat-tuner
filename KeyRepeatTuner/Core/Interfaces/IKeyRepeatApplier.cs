using KeyRepeatTuner.Configuration;

namespace KeyRepeatTuner.Core.Interfaces;

public interface IKeyRepeatApplier
{
    void Apply(KeyRepeatState state);
}