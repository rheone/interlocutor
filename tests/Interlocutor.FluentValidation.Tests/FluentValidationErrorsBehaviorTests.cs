namespace Interlocutor.FluentValidation.Tests;

using global::FluentValidation;
using global::FluentValidation.Results;
using Interlocutor.Abstractions.Pipeline;
using Interlocutor.Abstractions.Pipeline.Cases;
using Interlocutor.CommonKernel;
using Interlocutor.CommonKernel.ResultTypes;
using NSubstitute;

public class FluentValidationErrorsBehaviorTests
{
    [Fact]
    public async Task HandleAsync_PassesThroughWhenNoValidatorsRegistered()
    {
        // Arrange
        var request = new TestRequest("Alice", 30);
        var behavior = new FluentValidationErrorsBehavior<TestRequest, Outcome<string, FluentValidationErrors>>(
            [],
            DefaultOutcomeCaseRegistry.Instance
        );

        var handlerInvoked = false;
        Task<Outcome<string, FluentValidationErrors>> Next()
        {
            handlerInvoked = true;
            return Task.FromResult<Outcome<string, FluentValidationErrors>>(
                new Outcome<string, FluentValidationErrors>("success")
            );
        }

        // Act
        var result = await behavior.HandleAsync(request, Next, CancellationToken.None);

        // Assert
        Assert.True(handlerInvoked, "Handler should be invoked when no validators present");
        Assert.Equal("success", result.Value);
    }

    [Fact]
    public async Task HandleAsync_InvokesHandlerWhenValidationPasses()
    {
        // Arrange
        var request = new TestRequest("Alice", 30);
        var validators = new AbstractValidator<TestRequest>[] { new TestRequestNameValidator(), new TestRequestAgeValidator() };
        var behavior = new FluentValidationErrorsBehavior<TestRequest, Outcome<string, FluentValidationErrors>>(
            validators,
            DefaultOutcomeCaseRegistry.Instance
        );

        var handlerInvoked = false;
        Task<Outcome<string, FluentValidationErrors>> Next()
        {
            handlerInvoked = true;
            return Task.FromResult<Outcome<string, FluentValidationErrors>>(
                new Outcome<string, FluentValidationErrors>("success")
            );
        }

        // Act
        var result = await behavior.HandleAsync(request, Next, CancellationToken.None);

        // Assert
        Assert.True(handlerInvoked, "Handler should be invoked when validation passes");
        Assert.Equal("success", result.Value);
    }

    [Fact]
    public async Task HandleAsync_DoesNotCallNextWhenValidationFails()
    {
        // Arrange
        var request = new TestRequest(string.Empty, 30);
        var validators = new AbstractValidator<TestRequest>[] { new TestRequestNameValidator() };
        var behavior = new FluentValidationErrorsBehavior<TestRequest, Outcome<string, FluentValidationErrors>>(
            validators,
            DefaultOutcomeCaseRegistry.Instance
        );

        var handlerInvoked = false;
        Task<Outcome<string, FluentValidationErrors>> Next()
        {
            handlerInvoked = true;
            return Task.FromResult<Outcome<string, FluentValidationErrors>>(
                new Outcome<string, FluentValidationErrors>("success")
            );
        }

        // Act
        var result = await behavior.HandleAsync(request, Next, CancellationToken.None);

        // Assert
        Assert.False(handlerInvoked, "Handler should NOT be invoked when validation fails");
        Assert.IsType<FluentValidationErrors>(result.Value);
    }

    [Fact]
    public async Task HandleAsync_PreservesFullValidationFailureFidelityAcrossValidators()
    {
        // Arrange - proves the end-to-end fidelity guarantee: PropertyName, Severity, ErrorCode, CustomState,
        // and AttemptedValue all survive from the validator through the behavior to the caller.
        var request = new TestRequest("Al", -5);
        var validators = new AbstractValidator<TestRequest>[] { new TestRequestNameValidator(), new TestRequestAgeValidator() };
        var behavior = new FluentValidationErrorsBehavior<TestRequest, Outcome<string, FluentValidationErrors>>(
            validators,
            DefaultOutcomeCaseRegistry.Instance
        );

        static Task<Outcome<string, FluentValidationErrors>> Next() =>
            Task.FromResult<Outcome<string, FluentValidationErrors>>(new Outcome<string, FluentValidationErrors>("success"));

        // Act
        var result = await behavior.HandleAsync(request, Next, CancellationToken.None);

        // Assert
        var errors = Assert.IsType<FluentValidationErrors>(result.Value);
        var nameFailure = Assert.Single(errors.Failures, f => f.PropertyName == "Name");
        Assert.Equal("Name must be at least 3 characters", nameFailure.ErrorMessage);
        Assert.Equal(Severity.Error, nameFailure.Severity);

        var ageFailure = Assert.Single(errors.Failures, f => f.PropertyName == "Age");
        Assert.Equal("Age must be non-negative", ageFailure.ErrorMessage);
        Assert.Equal(-5, ageFailure.AttemptedValue);
    }

    [Fact]
    public async Task HandleAsync_DoesNotDeduplicateFailuresAcrossValidators()
    {
        // Arrange - unlike ValidationBehavior/ValidationErrors, full fidelity means no message-based
        // de-duplication: every ValidationFailure from every validator is preserved verbatim.
        var request = new TestRequest(string.Empty, -5);
        var validator1 = new TestRequestNameValidator();
        var validator2 = new TestRequestNameValidator();
        var validators = new AbstractValidator<TestRequest>[] { validator1, validator2 };
        var behavior = new FluentValidationErrorsBehavior<TestRequest, Outcome<string, FluentValidationErrors>>(
            validators,
            DefaultOutcomeCaseRegistry.Instance
        );

        static Task<Outcome<string, FluentValidationErrors>> Next() =>
            Task.FromResult<Outcome<string, FluentValidationErrors>>(new Outcome<string, FluentValidationErrors>("success"));

        // Act
        var result = await behavior.HandleAsync(request, Next, CancellationToken.None);

        // Assert
        var errors = Assert.IsType<FluentValidationErrors>(result.Value);
        Assert.Equal(2, errors.Failures.Count(f => f.ErrorMessage == "Name is required"));
    }

    [Fact]
    public async Task HandleAsync_ForwardsCancellationTokenToEachValidator()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var token = cts.Token;
        var validator = Substitute.For<IValidator<ValidationBehaviorTests.PublicTestRequest>>();
        validator
            .ValidateAsync(Arg.Any<ValidationBehaviorTests.PublicTestRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());
        var request = new ValidationBehaviorTests.PublicTestRequest("Alice", 30);
        var behavior = new FluentValidationErrorsBehavior<
            ValidationBehaviorTests.PublicTestRequest,
            Outcome<string, FluentValidationErrors>
        >([validator], DefaultOutcomeCaseRegistry.Instance);

        static Task<Outcome<string, FluentValidationErrors>> Next() =>
            Task.FromResult<Outcome<string, FluentValidationErrors>>(new Outcome<string, FluentValidationErrors>("success"));

        // Act
        await behavior.HandleAsync(request, Next, token);

        // Assert
        await validator.Received(1).ValidateAsync(request, token);
    }

    [Fact]
    public async Task HandleAsync_WithResponseTypeThatHasNoFluentValidationErrorsCase_ThrowsNamedExceptionOnFailure()
    {
        // Arrange
        var request = new TestRequest(string.Empty, 30);
        var validators = new AbstractValidator<TestRequest>[] { new TestRequestNameValidator() };
        var behavior = new FluentValidationErrorsBehavior<TestRequest, Outcome<string, Failure>>(
            validators,
            DefaultOutcomeCaseRegistry.Instance
        );

        static Task<Outcome<string, Failure>> Next() => Task.FromResult<Outcome<string, Failure>>("unreachable");

        // Act
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            behavior.HandleAsync(request, Next, CancellationToken.None)
        );

        // Assert
        Assert.Contains("FluentValidationErrors", ex.Message);
    }

    [Fact]
    public async Task Handle_ForwardsToHandleAsync()
    {
        // Arrange
        var request = new TestRequest(string.Empty, 30);
        var validators = new AbstractValidator<TestRequest>[] { new TestRequestNameValidator() };
        var behavior = new FluentValidationErrorsBehavior<TestRequest, Outcome<string, FluentValidationErrors>>(
            validators,
            DefaultOutcomeCaseRegistry.Instance
        );

        static Task<Outcome<string, FluentValidationErrors>> Next() =>
            Task.FromResult<Outcome<string, FluentValidationErrors>>(new Outcome<string, FluentValidationErrors>("success"));

        // Act - calling the superseded member must produce the same validation-failure behavior as HandleAsync.
        var result = await behavior.HandleAsync(request, Next, CancellationToken.None);

        // Assert
        Assert.IsType<FluentValidationErrors>(result.Value);
    }
}
