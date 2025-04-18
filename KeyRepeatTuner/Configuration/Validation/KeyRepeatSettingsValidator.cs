using FluentValidation;

namespace KeyRepeatTuner.Configuration.Validation;

public class KeyRepeatSettingsValidator : AbstractValidator<KeyRepeatSettings>
{
    public KeyRepeatSettingsValidator()
    {
        RuleFor(x => x.Default)
            .NotNull().WithMessage("Default key repeat settings must be provided.")
            .SetValidator(CreateKeyRepeatStateValidator("Default"));

        RuleFor(x => x.FastMode)
            .NotNull().WithMessage("FastMode key repeat settings must be provided.")
            .SetValidator(CreateKeyRepeatStateValidator("FastMode"));
    }

    private static InlineValidator<KeyRepeatState> CreateKeyRepeatStateValidator(string prefix)
    {
        var validator = new InlineValidator<KeyRepeatState>();

        validator.RuleFor(s => s.RepeatSpeed)
            .IsRepeatSpeed()
            .WithName($"{prefix}.RepeatSpeed");

        validator.RuleFor(s => s.RepeatDelay)
            .IsRepeatDelay()
            .WithName($"{prefix}.RepeatDelay");

        return validator;
    }
}