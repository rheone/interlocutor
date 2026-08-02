using FluentValidation;
using Mediance.Abstractions.Commands;

namespace Mediance.FluentValidation.Tests;

public record TestRequest(string name) : ICommand<string>;

public class TestRequestValidator : AbstractValidator<TestRequest>
{
    public TestRequestValidator()
    {
        this.RuleFor(x => x.name).NotEmpty();
    }
}

public class ValidationBehaviorTests
{
    [Fact(Skip = "ValidationBehavior has been commented out during code migration")]
    public async Task NoValidators_PassesThrough()
    {
        // ValidationBehavior<TRequest, TSuccess> is not available in current version
        await Task.CompletedTask;
    }

    [Fact(Skip = "ValidationBehavior has been commented out during code migration")]
    public async Task ValidationFailure_ReturnsValidationFailed()
    {
        // ValidationBehavior<TRequest, TSuccess> is not available in current version
        await Task.CompletedTask;
    }
}
