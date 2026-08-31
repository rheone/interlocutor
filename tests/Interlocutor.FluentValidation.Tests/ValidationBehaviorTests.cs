namespace Interlocutor.FluentValidation.Tests;

using global::FluentValidation;
using global::FluentValidation.Results;
using Interlocutor.Abstractions.Pipeline;
using Interlocutor.Abstractions.Pipeline.Cases;
using Interlocutor.CommonKernel;
using Interlocutor.CommonKernel.ResultTypes;
using NSubstitute;

public class ValidationBehaviorTests
{
    [Fact]
    public async Task Handle_PassesThroughWhenNoValidatorsRegistered()
    {
        // Arrange
        var request = new TestRequest("Alice", 30);
        var behavior = new ValidationBehavior<TestRequest, Outcome<string, ValidationErrors>>(
            [],
            DefaultOutcomeCaseRegistry.Instance
        );

        var handlerInvoked = false;
        async Task<Outcome<string, ValidationErrors>> Next()
        {
            handlerInvoked = true;
            return await Task.FromResult<Outcome<string, ValidationErrors>>(new Outcome<string, ValidationErrors>("success"))
                .ConfigureAwait(false);
        }

        // Act
        var result = await behavior.Handle(request, Next, CancellationToken.None);

        // Assert
        Assert.True(handlerInvoked, "Handler should be invoked when no validators present");
        Assert.Equal("success", result.Value);
    }

    [Fact]
    public async Task Handle_InvokesHandlerWhenValidationPasses()
    {
        // Arrange
        var request = new TestRequest("Alice", 30);
        var validators = new AbstractValidator<TestRequest>[] { new TestRequestNameValidator(), new TestRequestAgeValidator() };
        var behavior = new ValidationBehavior<TestRequest, Outcome<string, ValidationErrors>>(
            validators,
            DefaultOutcomeCaseRegistry.Instance
        );

        var handlerInvoked = false;
        async Task<Outcome<string, ValidationErrors>> Next()
        {
            handlerInvoked = true;
            return await Task.FromResult<Outcome<string, ValidationErrors>>(new Outcome<string, ValidationErrors>("success"))
                .ConfigureAwait(false);
        }

        // Act
        var result = await behavior.Handle(request, Next, CancellationToken.None);

        // Assert
        Assert.True(handlerInvoked, "Handler should be invoked when validation passes");
        Assert.Equal("success", result.Value);
    }

    [Fact]
    public async Task Handle_DoesNotCallNextWhenValidationFails()
    {
        // Arrange
        var request = new TestRequest(string.Empty, 30);
        var validators = new AbstractValidator<TestRequest>[] { new TestRequestNameValidator() };
        var behavior = new ValidationBehavior<TestRequest, Outcome<string, ValidationErrors>>(
            validators,
            DefaultOutcomeCaseRegistry.Instance
        );

        var handlerInvoked = false;
        async Task<Outcome<string, ValidationErrors>> Next()
        {
            handlerInvoked = true;
            return await Task.FromResult<Outcome<string, ValidationErrors>>(new Outcome<string, ValidationErrors>("success"))
                .ConfigureAwait(false);
        }

        // Act
        var result = await behavior.Handle(request, Next, CancellationToken.None);

        // Assert
        Assert.False(handlerInvoked, "Handler should NOT be invoked when validation fails");
        Assert.IsType<ValidationErrors>(result.Value);
    }

    [Fact]
    public async Task Handle_ReturnsAllErrorMessagesAcrossValidatorsWhenValidationFails()
    {
        // Arrange
        var request = new TestRequest("Al", -5);
        var validators = new AbstractValidator<TestRequest>[] { new TestRequestNameValidator(), new TestRequestAgeValidator() };
        var behavior = new ValidationBehavior<TestRequest, Outcome<string, ValidationErrors>>(
            validators,
            DefaultOutcomeCaseRegistry.Instance
        );

        static async Task<Outcome<string, ValidationErrors>> Next()
        {
            return await Task.FromResult<Outcome<string, ValidationErrors>>(new Outcome<string, ValidationErrors>("success"))
                .ConfigureAwait(false);
        }

        // Act
        var result = await behavior.Handle(request, Next, CancellationToken.None);

        // Assert
        var validationErrors = Assert.IsType<ValidationErrors>(result.Value);
        Assert.True(validationErrors.Count > 0, "Should have validation errors");

        // Check that all error messages are present
        var messages = validationErrors.Messages;
        Assert.Contains("Name must be at least 3 characters", messages);
        Assert.Contains("Age must be non-negative", messages);
    }

    [Fact]
    public async Task Handle_DeDuplicatesErrorMessagesAcrossValidators()
    {
        // Arrange
        var request = new TestRequest(string.Empty, -5);
        var validator1 = new TestRequestNameValidator();
        var validator2 = new TestRequestNameValidator(); // Same validator twice
        var validators = new AbstractValidator<TestRequest>[] { validator1, validator2 };
        var behavior = new ValidationBehavior<TestRequest, Outcome<string, ValidationErrors>>(
            validators,
            DefaultOutcomeCaseRegistry.Instance
        );

        static async Task<Outcome<string, ValidationErrors>> Next()
        {
            return await Task.FromResult<Outcome<string, ValidationErrors>>(new Outcome<string, ValidationErrors>("success"))
                .ConfigureAwait(false);
        }

        // Act
        var result = await behavior.Handle(request, Next, CancellationToken.None);

        // Assert
        var validationErrors = Assert.IsType<ValidationErrors>(result.Value);
        var messages = validationErrors.Messages;

        // "Name is required" should appear only once despite two validators producing it
        Assert.Single(messages, m => m == "Name is required");
    }

    // Auto Generated, verify expected behavior:
    [Fact]
    public async Task Handle_PreservesOrderOfFirstOccurrenceAcrossValidators()
    {
        // Arrange
        var request = new TestRequest(string.Empty, -5);
        var validators = new AbstractValidator<TestRequest>[] { new TestRequestNameValidator(), new TestRequestAgeValidator() };
        var behavior = new ValidationBehavior<TestRequest, Outcome<string, ValidationErrors>>(
            validators,
            DefaultOutcomeCaseRegistry.Instance
        );

        static Task<Outcome<string, ValidationErrors>> Next()
        {
            return Task.FromResult<Outcome<string, ValidationErrors>>(new Outcome<string, ValidationErrors>("success"));
        }

        // Act
        var result = await behavior.Handle(request, Next, CancellationToken.None);

        // Assert - name validator runs first, so its errors ("Name is required") must precede the age
        // validator's ("Age must be non-negative") in the aggregated message list.
        var validationErrors = Assert.IsType<ValidationErrors>(result.Value);
        var messages = validationErrors.Messages.ToList();
        var nameErrorIndex = messages.IndexOf("Name is required");
        var ageErrorIndex = messages.IndexOf("Age must be non-negative");
        Assert.True(nameErrorIndex >= 0 && ageErrorIndex >= 0, "Both error messages should be present");
        Assert.True(nameErrorIndex < ageErrorIndex, "Errors should preserve order of first occurrence across validators");
    }

    // Auto Generated, verify expected behavior:
    [Fact]
    public async Task Handle_ForwardsCancellationTokenToEachValidator()
    {
        // Arrange
        // NSubstitute/Castle DynamicProxy can only proxy IValidator<T> when T is public (an internal request
        // type isn't accessible to the generated proxy assembly), so this test uses a public record rather
        // than the internal TestRequest used elsewhere in this file.
        using var cts = new CancellationTokenSource();
        var token = cts.Token;
        var validator = Substitute.For<IValidator<PublicTestRequest>>();
        validator.ValidateAsync(Arg.Any<PublicTestRequest>(), Arg.Any<CancellationToken>()).Returns(new ValidationResult());
        var request = new PublicTestRequest("Alice", 30);
        var behavior = new ValidationBehavior<PublicTestRequest, Outcome<string, ValidationErrors>>(
            [validator],
            DefaultOutcomeCaseRegistry.Instance
        );

        static Task<Outcome<string, ValidationErrors>> Next()
        {
            return Task.FromResult<Outcome<string, ValidationErrors>>(new Outcome<string, ValidationErrors>("success"));
        }

        // Act
        await behavior.Handle(request, Next, token);

        // Assert
        await validator.Received(1).ValidateAsync(request, token);
    }

    [Fact]
    public async Task Handle_WithResponseTypeThatHasNoValidationErrorsCase_ThrowsNamedExceptionOnFailure()
    {
        // Arrange
        // TResponse is no longer decomposed at compile time into a success-type parameter with
        // ValidationErrors pinned as the failure case - it's an arbitrary IOutcome the caller
        // chose, constructed reflectively from a ValidationErrors case constructor when validation fails.
        // A response shape with no such case can't express "failed validation" at all, so it must fail
        // loudly and specifically rather than silently succeeding or crashing obscurely.
        var request = new TestRequest(string.Empty, 30);
        var validators = new AbstractValidator<TestRequest>[] { new TestRequestNameValidator() };
        var behavior = new ValidationBehavior<TestRequest, Outcome<string, Failure>>(
            validators,
            DefaultOutcomeCaseRegistry.Instance
        );

        static Task<Outcome<string, Failure>> Next()
        {
            return Task.FromResult<Outcome<string, Failure>>("unreachable");
        }

        // Act
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            behavior.Handle(request, Next, CancellationToken.None)
        );

        // Assert
        Assert.Contains("ValidationErrors", ex.Message);
    }

    [Fact]
    public async Task Handle_HandRolledNonUnionResponse_ShortCircuitsWithValidationErrorsViaCustomRegistry()
    {
        // Arrange
        // TResponse need not be a compiler-generated union at all - IOutcomeCaseRegistry is the seam that
        // lets ValidationBehavior construct a hand-rolled IOutcome's ValidationErrors case via a
        // consumer-registered factory, rather than requiring UnionCaseFactory's reflection to find a case
        // constructor that a hand-rolled type simply doesn't have.
        var registry = new OutcomeCaseRegistryBuilder()
            .RegisterFactory<HandRolledOutcome, string>(HandRolledOutcome.FromSuccess)
            .RegisterFactory<HandRolledOutcome, ValidationErrors>(HandRolledOutcome.FromValidationErrors)
            .Build();

        var request = new TestRequest(string.Empty, 30);
        var validators = new AbstractValidator<TestRequest>[] { new TestRequestNameValidator() };
        var behavior = new ValidationBehavior<TestRequest, HandRolledOutcome>(validators, registry);

        var handlerInvoked = false;
        Task<HandRolledOutcome> Next()
        {
            handlerInvoked = true;
            return Task.FromResult(HandRolledOutcome.FromSuccess("unreachable"));
        }

        // Act
        var result = await behavior.Handle(request, Next, CancellationToken.None);

        // Assert
        Assert.False(handlerInvoked, "Handler should NOT be invoked when validation fails");
        var validationErrors = Assert.IsType<ValidationErrors>(result.CaseValue);
        Assert.Contains("Name is required", validationErrors.Messages);
    }

    /// <summary>A hand-rolled, non-union <see cref="IOutcome"/> implementation with no
    /// compiler-generated case constructor for <see cref="UnionCaseFactory"/> to find reflectively - proves
    /// <see cref="ValidationBehavior{TRequest, TResponse}"/> goes through the injected
    /// <see cref="IOutcomeCaseRegistry"/> rather than assuming the response type is a union.</summary>
    public sealed class HandRolledOutcome : IOutcome
    {
        private HandRolledOutcome(Type caseType, object? caseValue)
        {
            this.CaseType = caseType;
            this.CaseValue = caseValue;
        }

        public Type CaseType { get; }

        public object? CaseValue { get; }

        public static HandRolledOutcome FromSuccess(string value) => new(typeof(string), value);

        public static HandRolledOutcome FromValidationErrors(ValidationErrors errors) => new(typeof(ValidationErrors), errors);
    }

    /// <summary>A public stand-in for <see cref="TestRequest"/>, needed only where NSubstitute must proxy
    /// <see cref="IValidator{T}"/> over the request type - Castle DynamicProxy cannot proxy a generic
    /// interface closed over an internal type argument.</summary>
    public sealed record PublicTestRequest(string Name, int Age);
}
