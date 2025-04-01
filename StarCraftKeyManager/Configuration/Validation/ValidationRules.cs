using FluentValidation;

namespace StarCraftKeyManager.Configuration.Validation;

/// <summary>
///     Provides reusable FluentValidation rules for app configuration models.
/// </summary>
public static class ValidationRules
{
    /// <summary>
    ///     Validates that the key repeat speed is within the accepted range [0, 31].
    /// </summary>
    public static IRuleBuilderOptions<T, int> IsRepeatSpeed<T>(this IRuleBuilder<T, int> ruleBuilder)
    {
        return ruleBuilder
            .InclusiveBetween(0, 31)
            .WithMessage("RepeatSpeed must be between 0 and 31.");
    }

    /// <summary>
    ///     Validates that the key repeat delay is within the accepted range [250, 1000] milliseconds.
    /// </summary>
    public static IRuleBuilderOptions<T, int> IsRepeatDelay<T>(this IRuleBuilder<T, int> ruleBuilder)
    {
        return ruleBuilder
            .InclusiveBetween(250, 1000)
            .WithMessage("RepeatDelay must be between 250ms and 1000ms.");
    }

    /// <summary>
    ///     Validates ergonomic rule: RepeatSpeed should be lower than RepeatDelay.
    ///     This is a soft constraint to prevent sluggish or extreme combos.
    /// </summary>
    public static IRuleBuilderOptions<T, KeyRepeatState> HasErgonomicBalance<T>(
        this IRuleBuilder<T, KeyRepeatState> ruleBuilder)
    {
        return ruleBuilder.Must(state =>
                state.RepeatSpeed * 30 < state.RepeatDelay)
            .WithMessage("RepeatSpeed * 30 should be less than RepeatDelay for better ergonomics.");
    }
}