namespace TodoTasks.Api.Tests.Unit.Infrastructure;

using Interlocutor.Abstractions.Core;
using Interlocutor.Persistence.Abstractions;
using NSubstitute;
using TodoTasks.Api.Domain;
using TodoTasks.Api.Features.Tasks.DeleteTodoTask;
using TodoTasks.Api.Infrastructure;

/// <summary>Unit tests for <see cref="TodoTasksPolicyEvaluator"/>.</summary>
public sealed class TodoTasksPolicyEvaluatorTests
{
    private readonly IDomainRepository<TodoTask, int> tasks = Substitute.For<IDomainRepository<TodoTask, int>>();
    private readonly IRequestContextAccessor contextAccessor = Substitute.For<IRequestContextAccessor>();
    private readonly TodoTasksPolicyEvaluator evaluator;

    public TodoTasksPolicyEvaluatorTests() => this.evaluator = new TodoTasksPolicyEvaluator(this.tasks, this.contextAccessor);

    [Fact]
    public async Task EvaluateAsync_OwnsTaskPolicy_CallerOwnsTheTask_ReturnsTrue_Test()
    {
        // Arrange
        var task = new TodoTask("Buy milk", "owner-1", null) { Id = 42 };
        this.tasks.GetById(42).Returns(task);
        this.contextAccessor.Context.Returns(new RequestContext { UserId = "owner-1" });

        // Act
        var authorized = await this.evaluator.EvaluateAsync(
            TodoTasksPolicyEvaluator.OwnsTaskPolicyName,
            new DeleteTodoTaskCommand(42),
            CancellationToken.None
        );

        // Assert
        Assert.True(authorized);
    }

    [Fact]
    public async Task EvaluateAsync_OwnsTaskPolicy_CallerDoesNotOwnTheTask_ReturnsFalse_Test()
    {
        // Arrange
        var task = new TodoTask("Buy milk", "owner-1", null) { Id = 42 };
        this.tasks.GetById(42).Returns(task);
        this.contextAccessor.Context.Returns(new RequestContext { UserId = "owner-2" });

        // Act
        var authorized = await this.evaluator.EvaluateAsync(
            TodoTasksPolicyEvaluator.OwnsTaskPolicyName,
            new DeleteTodoTaskCommand(42),
            CancellationToken.None
        );

        // Assert
        Assert.False(authorized);
    }

    [Fact]
    public async Task EvaluateAsync_OwnsTaskPolicy_NoTaskWithThatId_ReturnsTrue_LeavingNotFoundToTheHandler_Test()
    {
        // Arrange - this evaluator's job is "does the caller own this task, if it exists"; a missing
        // task is deliberately not this policy's call to deny, so DeleteTodoTaskHandler is the one that
        // ends up returning NotFound rather than this check masking it as Forbidden.
        this.tasks.GetById(42).Returns((TodoTask?)null);
        this.contextAccessor.Context.Returns(new RequestContext { UserId = "owner-1" });

        // Act
        var authorized = await this.evaluator.EvaluateAsync(
            TodoTasksPolicyEvaluator.OwnsTaskPolicyName,
            new DeleteTodoTaskCommand(42),
            CancellationToken.None
        );

        // Assert
        Assert.True(authorized);
    }

    [Fact]
    public async Task EvaluateAsync_OwnsTaskPolicy_NoAuthenticatedCaller_ReturnsFalse_Test()
    {
        // Arrange
        var task = new TodoTask("Buy milk", "owner-1", null) { Id = 42 };
        this.tasks.GetById(42).Returns(task);
        this.contextAccessor.Context.Returns((RequestContext?)null);

        // Act
        var authorized = await this.evaluator.EvaluateAsync(
            TodoTasksPolicyEvaluator.OwnsTaskPolicyName,
            new DeleteTodoTaskCommand(42),
            CancellationToken.None
        );

        // Assert
        Assert.False(authorized);
    }

    [Fact]
    public async Task EvaluateAsync_UnknownPolicy_ReturnsFalse_Test()
    {
        // Arrange
        var task = new TodoTask("Buy milk", "owner-1", null) { Id = 42 };
        this.tasks.GetById(42).Returns(task);
        this.contextAccessor.Context.Returns(new RequestContext { UserId = "owner-1" });

        // Act
        var authorized = await this.evaluator.EvaluateAsync(
            "SomeOtherPolicy",
            new DeleteTodoTaskCommand(42),
            CancellationToken.None
        );

        // Assert
        Assert.False(authorized);
    }
}
