using FluentValidation;

namespace StarCraftKeyManager.Configuration.Validation;

public class AppSettingsValidator : AbstractValidator<AppSettings>
{
    public AppSettingsValidator()
    {
        RuleFor(x => x.ProcessNames)
            .NotEmpty().WithMessage("At least one process name must be specified.");

        RuleFor(x => x.KeyRepeat)
            .NotNull().WithMessage("KeyRepeat settings must be provided.")
            .SetValidator(new KeyRepeatSettingsValidator());
    }

    private class KeyRepeatSettingsValidator : AbstractValidator<KeyRepeatSettings>
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
}