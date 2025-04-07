using FluentValidation;

namespace KeyRepeatTuner.Configuration.Validation;

public class KeyRepeatSettingsValidator : AbstractValidator<KeyRepeatSettings>
{
    public KeyRepeatSettingsValidator()
    {
        RuleFor(x => x.Default)
            .NotNull().WithMessage("Default key repeat settings must be provided.")
            .DependentRules(() =>
            {
                RuleFor(x => x.Default.RepeatSpeed).IsRepeatSpeed();
                RuleFor(x => x.Default.RepeatDelay).IsRepeatDelay();
            });

        RuleFor(x => x.FastMode)
            .NotNull().WithMessage("FastMode key repeat settings must be provided.")
            .DependentRules(() =>
            {
                RuleFor(x => x.FastMode.RepeatSpeed).IsRepeatSpeed();
                RuleFor(x => x.FastMode.RepeatDelay).IsRepeatDelay();
            });
    }
}