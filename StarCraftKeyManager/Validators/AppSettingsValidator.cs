using FluentValidation;
using StarCraftKeyManager.Models;

namespace StarCraftKeyManager.Validators;

public class AppSettingsValidator : AbstractValidator<AppSettings>
{
    public AppSettingsValidator()
    {
        RuleFor(x => x.KeyRepeat.Default.RepeatSpeed)
            .InclusiveBetween(0, 31)
            .WithMessage("RepeatSpeed must be between 0 and 31.");

        RuleFor(x => x.KeyRepeat.Default.RepeatDelay)
            .InclusiveBetween(250, 1000)
            .WithMessage("RepeatDelay must be between 250ms and 1000ms.");

        RuleFor(x => x.KeyRepeat.FastMode.RepeatSpeed)
            .InclusiveBetween(0, 31)
            .WithMessage("RepeatSpeed must be between 0 and 31.");

        RuleFor(x => x.KeyRepeat.FastMode.RepeatDelay)
            .InclusiveBetween(250, 1000)
            .WithMessage("RepeatDelay must be between 250ms and 1000ms.");
    }
}