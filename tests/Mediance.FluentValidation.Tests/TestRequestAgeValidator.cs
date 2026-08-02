namespace Mediance.FluentValidation.Tests;

using global::FluentValidation;

/// <summary>Validator for TestRequest.Age field.</summary>
internal class TestRequestAgeValidator : AbstractValidator<TestRequest>
{
    /// <summary>Initializes a new instance of the <see cref="TestRequestAgeValidator"/> class.</summary>
    public TestRequestAgeValidator()
    {
        this.RuleFor(x => x.Age)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Age must be non-negative")
            .LessThanOrEqualTo(150)
            .WithMessage("Age must be realistic");
    }
}
