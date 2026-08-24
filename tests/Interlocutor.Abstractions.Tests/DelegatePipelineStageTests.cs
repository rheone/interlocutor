namespace Interlocutor.Abstractions.Tests;

using Interlocutor.Abstractions.Pipeline;
using Interlocutor.CommonKernel;
using Interlocutor.CommonKernel.ResultTypes;

/// <summary>Tests for <see cref="DelegatePipelineStage{TRequest, TResponse}"/>.</summary>
public class DelegatePipelineStageTests
{
    // Auto Generated, verify expected behavior:
    [Fact]
    public async Task OnBeforeAsync_WithSuppliedDelegate_InvokesIt()
    {
        // Arrange
        var expected = StageResult<OperationOutcome<string, Failure>>.ShortCircuit(new Failure("declined"));
        var stage = new DelegatePipelineStage<TestRequest, OperationOutcome<string, Failure>>(
            (_, _) => Task.FromResult(expected),
            onAfter: null
        );

        // Act
        var result = await stage.OnBeforeAsync(new TestRequest(), CancellationToken.None);

        // Assert
        Assert.True(result.ShortCircuited);
        Assert.Equal(expected.Response, result.Response);
    }

    // Auto Generated, verify expected behavior:
    [Fact]
    public async Task OnBeforeAsync_WithNoSuppliedDelegate_FallsBackToBaseNoOpContinue()
    {
        // Arrange
        var stage = new DelegatePipelineStage<TestRequest, OperationOutcome<string, Failure>>(onBefore: null, onAfter: null);

        // Act
        var result = await stage.OnBeforeAsync(new TestRequest(), CancellationToken.None);

        // Assert
        Assert.False(result.ShortCircuited);
    }

    // Auto Generated, verify expected behavior:
    [Fact]
    public async Task OnAfterAsync_WithSuppliedDelegate_InvokesItWithRequestAndResult()
    {
        // Arrange
        TestRequest? observedRequest = null;
        OperationOutcome<string, Failure>? observedResult = null;
        var stage = new DelegatePipelineStage<TestRequest, OperationOutcome<string, Failure>>(
            onBefore: null,
            (request, result, _) =>
            {
                observedRequest = request;
                observedResult = result;
                return Task.CompletedTask;
            }
        );
        var request = new TestRequest();
        OperationOutcome<string, Failure> response = "done";

        // Act
        await stage.OnAfterAsync(request, response, CancellationToken.None);

        // Assert
        Assert.Same(request, observedRequest);
        Assert.Equal(response, observedResult);
    }

    // Auto Generated, verify expected behavior:
    [Fact]
    public async Task OnAfterAsync_WithNoSuppliedDelegate_CompletesAsANoOp()
    {
        // Arrange
        var stage = new DelegatePipelineStage<TestRequest, OperationOutcome<string, Failure>>(onBefore: null, onAfter: null);

        // Act & Assert - completing without throwing proves the base no-op hook ran.
        await stage.OnAfterAsync(new TestRequest(), "done", CancellationToken.None);
    }

    public record TestRequest;
}
