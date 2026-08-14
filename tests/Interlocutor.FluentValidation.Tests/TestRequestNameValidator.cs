namespace Interlocutor.FluentValidation.Tests;

using global::FluentValidation;

/// <summary>Validator for TestRequest.Name field.</summary>
internal class TestRequestNameValidator : AbstractValidator<TestRequest>
{
    /// <summary>Initializes a new instance of the <see cref="TestRequestNameValidator"/> class.</summary>
    public TestRequestNameValidator()
    {
        this.RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Name is required")
            .MinimumLength(3)
            .WithMessage("Name must be at least 3 characters");
    }
}
