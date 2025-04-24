using FluentValidation;

namespace KeyRepeatTuner.Configuration.Validation;

internal class AppSettingsValidator : AbstractValidator<AppSettings>
{
    public AppSettingsValidator()
    {
        RuleFor(x => x.ProcessNames)
            .NotEmpty().WithMessage("At least one process name must be specified.");

        RuleForEach(x => x.ProcessNames)
            .NotNull().WithMessage("Process name in the list cannot be null.");

        RuleFor(x => x.KeyRepeat)
            .NotNull().WithMessage("KeyRepeat settings must be provided.")
            .SetValidator(new KeyRepeatSettingsValidator());
    }
}