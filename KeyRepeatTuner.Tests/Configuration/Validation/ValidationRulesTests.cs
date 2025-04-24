using FluentValidation;
using KeyRepeatTuner.Configuration.Validation;
using Xunit;

namespace KeyRepeatTuner.Tests.Configuration.Validation;

public class ValidationRulesTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(15)]
    [InlineData(31)]
    public void IsRepeatSpeed_ShouldPass_ValidValues(int speed)
    {
        var validator = new InlineValidator<SpeedTestModel>();
        validator.RuleFor(x => x.Speed).IsRepeatSpeed();

        var result = validator.Validate(new SpeedTestModel { Speed = speed });

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(32)]
    public void IsRepeatSpeed_ShouldFail_InvalidValues(int speed)
    {
        var validator = new InlineValidator<SpeedTestModel>();
        validator.RuleFor(x => x.Speed).IsRepeatSpeed();

        var result = validator.Validate(new SpeedTestModel { Speed = speed });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("RepeatSpeed must be between 0 and 31"));
    }

    [Theory]
    [InlineData(250)]
    [InlineData(500)]
    [InlineData(1000)]
    public void IsRepeatDelay_ShouldPass_ValidValues(int delay)
    {
        var validator = new InlineValidator<DelayTestModel>();
        validator.RuleFor(x => x.Delay).IsRepeatDelay();

        var result = validator.Validate(new DelayTestModel { Delay = delay });

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData(200)]
    [InlineData(1050)]
    public void IsRepeatDelay_ShouldFail_InvalidValues(int delay)
    {
        var validator = new InlineValidator<DelayTestModel>();
        validator.RuleFor(x => x.Delay).IsRepeatDelay();

        var result = validator.Validate(new DelayTestModel { Delay = delay });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("RepeatDelay must be between 250ms and 1000ms"));
    }

    private class SpeedTestModel
    {
        public int Speed { get; init; }
    }

    private class DelayTestModel
    {
        public int Delay { get; init; }
    }
}