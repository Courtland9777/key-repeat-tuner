using FluentValidation;
using Serilog;

namespace KeyRepeatTuner.Configuration.Validation;

public class AppSettingsChangeValidator
{
    private readonly IValidator<AppSettings> _validator;

    public AppSettingsChangeValidator(IValidator<AppSettings> validator)
    {
        _validator = validator;
    }

    public bool Validate(AppSettings newSettings)
    {
        var result = _validator.Validate(newSettings);

        if (result.IsValid)
            return true;

        foreach (var failure in result.Errors)
            Log.Error("Dynamic config validation failed: {Property} - {Message}", failure.PropertyName,
                failure.ErrorMessage);

        Log.Error("Configuration change rejected due to {Count} validation errors.", result.Errors.Count);
        return false;
    }
}