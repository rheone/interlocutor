using System.Diagnostics;
using FluentValidation;
using Mediance.Abstractions;
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
                return Task.FromResult<Result<string>>(new Result<string>("ok"));
            },
            CancellationToken.None
        );

        Assert.True(handled);
        var value = result.Value switch
        {
            string s => s,
            Failure => null,
            _ => throw new UnreachableException(),
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
            () => Task.FromResult<Result<string>>(new Result<string>("should not reach")),
            CancellationToken.None
        );

        var isFailure = result.Value switch
        {
            Failure => true,
            string => false,
            _ => throw new UnreachableException(),
        };
        Assert.True(isFailure);
    }
}
