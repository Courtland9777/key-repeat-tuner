using FluentValidation;
using StarCraftKeyManager.Utilities;

namespace StarCraftKeyManager.Configuration;

public class AppSettingsValidator : AbstractValidator<AppSettings>
{
    public AppSettingsValidator(ILogger<AppSettingsValidator>? logger = null)
    {
        RuleFor(x => x.ProcessName)
            .NotEmpty().WithMessage("ProcessName must be specified.")
            .Must(name =>
            {
                try
                {
                    _ = ProcessNameSanitizer.Normalize(name);
                    return true;
                }
                catch (Exception ex)
                {
                    logger?.LogError(ex, "Validation failed for ProcessName: {Input}", name);
                    return false;
                }
            })
            .WithMessage("ProcessName must be a valid process name (e.g., notepad, starcraft). Do not include '.exe'.");

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
                    RuleFor(x => x.Default!.RepeatSpeed)
                        .InclusiveBetween(0, 31)
                        .WithMessage("RepeatSpeed must be between 0 and 31.");

                    RuleFor(x => x.Default!.RepeatDelay)
                        .InclusiveBetween(250, 1000)
                        .WithMessage("RepeatDelay must be between 250ms and 1000ms.");
                });

            RuleFor(x => x.FastMode)
                .NotNull().WithMessage("FastMode key repeat settings must be provided.")
                .DependentRules(() =>
                {
                    RuleFor(x => x.FastMode!.RepeatSpeed)
                        .InclusiveBetween(0, 31)
                        .WithMessage("RepeatSpeed must be between 0 and 31.");

                    RuleFor(x => x.FastMode!.RepeatDelay)
                        .InclusiveBetween(250, 1000)
                        .WithMessage("RepeatDelay must be between 250ms and 1000ms.");
                });
        }
    }
}