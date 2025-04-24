using FluentValidation;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace KeyRepeatTuner.Infrastructure.Extensions;

public static class OptionsBuilderFluentValidationExtensions
{
    public static OptionsBuilder<T> ValidateFluently<T>(this OptionsBuilder<T> builder) where T : class
    {
        builder.Services.TryAddSingleton<IValidateOptions<T>>(sp =>
        {
            var validator = sp.GetRequiredService<IValidator<T>>();
            return new FluentValidationOptions<T>(validator);
        });

        return builder;
    }

    private class FluentValidationOptions<T> : IValidateOptions<T> where T : class
    {
        private readonly IValidator<T> _validator;

        public FluentValidationOptions(IValidator<T> validator)
        {
            _validator = validator;
        }

        public ValidateOptionsResult Validate(string? name, T options)
        {
            var result = _validator.Validate(options);
            if (result.IsValid)
                return ValidateOptionsResult.Success;

            var errors = result.Errors.Select(e => $"{e.PropertyName} - {e.ErrorMessage}");
            return ValidateOptionsResult.Fail(errors);
        }
    }
}