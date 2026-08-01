using FluentValidation;
using Mediance.Abstractions.Commands;
using Mediance.CommonKernel;

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
    [Fact]
    public async Task NoValidators_PassesThrough()
    {
        var behavior = new ValidationBehavior<TestRequest, string>([]);
        var handled = false;

        var result = await behavior.Handle(
            new TestRequest("test"),
            () =>
            {
                handled = true;
                return Task.FromResult<Result<string, ValidationFailed>>("ok");
            },
            CancellationToken.None
        );

        Assert.True(handled);
        var value = result switch
        {
            string s => s,
            ValidationFailed => null,
        };
        Assert.Equal("ok", value);
    }

    [Fact]
    public async Task ValidationFailure_ReturnsValidationFailed()
    {
        var validators = new[] { new TestRequestValidator() };
        var behavior = new ValidationBehavior<TestRequest, string>(validators);

        var result = await behavior.Handle(
            new TestRequest(""),
            () => Task.FromResult<Result<string, ValidationFailed>>("should not reach"),
            CancellationToken.None
        );

        var isFailure = result switch
        {
            ValidationFailed => true,
            string => false,
        };
        Assert.True(isFailure);
    }
}
