namespace TodoTasks.Api.Tests.Unit.Infrastructure;

using Interlocutor.Abstractions.Core;
using NSubstitute;
using TodoTasks.Api.Infrastructure;

/// <summary>Unit tests for <see cref="TodoTaskPolicyEvaluator"/>.</summary>
public sealed class TodoTaskPolicyEvaluatorTests
{
    private readonly IRequestContextAccessor contextAccessor = Substitute.For<IRequestContextAccessor>();

    [Fact]
    public async Task EvaluateAsync_DeleteTodoTaskPolicy_AuthenticatedCaller_ReturnsTrue_Test()
    {
        // Arrange
        this.contextAccessor.Context.Returns(new RequestContext { UserId = "42" });
        var evaluator = new TodoTaskPolicyEvaluator(this.contextAccessor);

        // Act
        var authorized = await evaluator.EvaluateAsync("DeleteTodoTask", new object(), CancellationToken.None);

        // Assert
        Assert.True(authorized);
    }

    [Fact]
    public async Task EvaluateAsync_DeleteTodoTaskPolicy_NoAuthenticatedCaller_ReturnsFalse_Test()
    {
        // Arrange
        this.contextAccessor.Context.Returns((RequestContext?)null);
        var evaluator = new TodoTaskPolicyEvaluator(this.contextAccessor);

        // Act
        var authorized = await evaluator.EvaluateAsync("DeleteTodoTask", new object(), CancellationToken.None);

        // Assert
        Assert.False(authorized);
    }

    [Fact]
    public async Task EvaluateAsync_UnknownPolicy_ReturnsFalse_Test()
    {
        // Arrange
        this.contextAccessor.Context.Returns(new RequestContext { UserId = "42" });
        var evaluator = new TodoTaskPolicyEvaluator(this.contextAccessor);

        // Act
        var authorized = await evaluator.EvaluateAsync("SomeOtherPolicy", new object(), CancellationToken.None);

        // Assert
        Assert.False(authorized);
    }
}
