namespace Mediance.FluentValidation.Tests;

using Mediance.Abstractions.Pipeline;
using Mediance.CommonKernel;
using Mediance.CommonKernel.ResultTypes;

public class ValidationBehaviorTests
{
    [Fact]
    public async Task NoValidators_PassesThrough()
    {
        var request = new TestRequest("Alice", 30);
        var behavior = new ValidationBehavior<TestRequest, string>([]);

        var handlerInvoked = false;
        var next = async () =>
        {
            handlerInvoked = true;
            return await Task.FromResult<OperationOutcome<string, ValidationErrors>>(new OperationOutcome<string, ValidationErrors>("success"));
        };

        var result = await behavior.Handle(request, next, CancellationToken.None);

        Assert.True(handlerInvoked, "Handler should be invoked when no validators present");
        Assert.Equal("success", result.Value);
    }

    [Fact]
    public async Task PassingValidation_InvokesHandler()
    {
        var request = new TestRequest("Alice", 30);
        var validators = new AbstractValidator<TestRequest>[]
        {
            new TestRequestNameValidator(),
            new TestRequestAgeValidator(),
        };
        var behavior = new ValidationBehavior<TestRequest, string>(validators);

        var handlerInvoked = false;
        var next = async () =>
        {
            handlerInvoked = true;
            return await Task.FromResult<OperationOutcome<string, ValidationErrors>>(new OperationOutcome<string, ValidationErrors>("success"));
        };

        var result = await behavior.Handle(request, next, CancellationToken.None);

        Assert.True(handlerInvoked, "Handler should be invoked when validation passes");
        Assert.Equal("success", result.Value);
    }

    [Fact]
    public async Task FailingValidation_ShortCircuitsHandler()
    {
        var request = new TestRequest(string.Empty, 30);
        var validators = new AbstractValidator<TestRequest>[]
        {
            new TestRequestNameValidator(),
        };
        var behavior = new ValidationBehavior<TestRequest, string>(validators);

        var handlerInvoked = false;
        var next = async () =>
        {
            handlerInvoked = true;
            return await Task.FromResult<OperationOutcome<string, ValidationErrors>>(new OperationOutcome<string, ValidationErrors>("success"));
        };

        var result = await behavior.Handle(request, next, CancellationToken.None);

        Assert.False(handlerInvoked, "Handler should NOT be invoked when validation fails");
        Assert.IsType<ValidationErrors>(result.Value);
    }

    [Fact]
    public async Task FailingValidation_ReturnsAllErrorMessages()
    {
        var request = new TestRequest("Al", -5);
        var validators = new AbstractValidator<TestRequest>[]
        {
            new TestRequestNameValidator(),
            new TestRequestAgeValidator(),
        };
        var behavior = new ValidationBehavior<TestRequest, string>(validators);

        var next = async () =>
        {
            return await Task.FromResult<OperationOutcome<string, ValidationErrors>>(new OperationOutcome<string, ValidationErrors>("success"));
        };

        var result = await behavior.Handle(request, next, CancellationToken.None);

        var validationErrors = Assert.IsType<ValidationErrors>(result.Value);
        Assert.True(validationErrors.Count > 0, "Should have validation errors");

        // Check that all error messages are present
        var messages = validationErrors.Messages;
        Assert.Contains("Name must be at least 3 characters", messages);
        Assert.Contains("Age must be non-negative", messages);
    }

    [Fact]
    public async Task DuplicateErrors_DeDuplicated()
    {
        var request = new TestRequest(string.Empty, -5);
        var validator1 = new TestRequestNameValidator();
        var validator2 = new TestRequestNameValidator(); // Same validator twice
        var validators = new AbstractValidator<TestRequest>[] { validator1, validator2 };
        var behavior = new ValidationBehavior<TestRequest, string>(validators);

        var next = async () =>
        {
            return await Task.FromResult<OperationOutcome<string, ValidationErrors>>(new OperationOutcome<string, ValidationErrors>("success"));
        };

        var result = await behavior.Handle(request, next, CancellationToken.None);

        var validationErrors = Assert.IsType<ValidationErrors>(result.Value);
        var messages = validationErrors.Messages;

        // "Name is required" should appear only once despite two validators producing it
        Assert.Single(messages, m => m == "Name is required");
    }

    [Fact]
    public async Task ValidationFailureMapsToCorrectionHTTPStatus()
    {
        var request = new TestRequest(string.Empty, 30);
        var validators = new AbstractValidator<TestRequest>[]
        {
            new TestRequestNameValidator(),
        };
        var behavior = new ValidationBehavior<TestRequest, string>(validators);

        var next = async () =>
        {
            return await Task.FromResult<OperationOutcome<string, ValidationErrors>>(new OperationOutcome<string, ValidationErrors>("success"));
        };

        var result = await behavior.Handle(request, next, CancellationToken.None);

        // Verify the result maps to the correct HTTP status via ResultExtensions
        var validationErrors = Assert.IsType<ValidationErrors>(result.Value);
        var httpResult = validationErrors.ToActionResult();

        var objectResult = Assert.IsType<UnprocessableEntityObjectResult>(httpResult);
        Assert.Equal(StatusCodes.Status422UnprocessableEntity, objectResult.StatusCode);
    }
}
