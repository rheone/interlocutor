namespace Interlocutor.Abstractions.Tests;

using System.Diagnostics;
using Interlocutor.Abstractions.Commands;
using Interlocutor.Abstractions.Core;
using Interlocutor.Abstractions.Notifications;
using Interlocutor.Abstractions.Pipeline;
using Interlocutor.Abstractions.Queries;
using Interlocutor.CommonKernel;
using Interlocutor.CommonKernel.ResultTypes;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Covers the "expand" half of the wide <c>Handle</c>/<c>Send</c>/<c>Publish</c>/<c>Cancel</c> rename: every new
/// <c>*Async</c>-suffixed dispatch member is a default interface implementation that forwards to the existing,
/// unchanged member. Every fake below implements only the pre-existing member - never the new one - to prove
/// that no implementer anywhere needs to change for the new member to work.
/// </summary>
public class AsyncDispatchMemberForwardingTests
{
    [Fact]
    public async Task ICommandHandler_HandleAsync_ForwardsToHandle()
    {
        // Arrange
        ICommandHandler<AsyncForwardingCommand, Outcome<int, Failure>> handler = new AsyncForwardingCommandHandler();

        // Act
        var result = await handler.HandleAsync(new AsyncForwardingCommand(21), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public async Task IQueryHandler_HandleAsync_ForwardsToHandle()
    {
        // Arrange
        IQueryHandler<AsyncForwardingQuery, Outcome<int, Failure>> handler = new AsyncForwardingQueryHandler();

        // Act
        var result = await handler.HandleAsync(new AsyncForwardingQuery(21), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public async Task INotificationHandler_HandleAsync_ForwardsToHandle()
    {
        // Arrange
        var received = new List<string>();
        INotificationHandler<AsyncForwardingNotification> handler = new AsyncForwardingNotificationHandler(received);

        // Act
        await handler.HandleAsync(new AsyncForwardingNotification("ping"), TestContext.Current.CancellationToken);

        // Assert - only Handle (not HandleAsync) was implemented, so this proves the default forwarded to it.
        Assert.Equal(["ping"], received);
    }

    [Fact]
    public async Task IPipelineBehavior_HandleAsync_ForwardsToHandle()
    {
        // Arrange
        IPipelineBehavior<AsyncForwardingRequest, Outcome<int, Failure>> behavior = new AsyncForwardingBehavior();
        Task<Outcome<int, Failure>> Next() => Task.FromResult(new Outcome<int, Failure>(1));

        // Act
        var result = await behavior.HandleAsync(new AsyncForwardingRequest(1), Next, TestContext.Current.CancellationToken);

        // Assert - the fake behavior doubles whatever the next delegate returns, proving HandleAsync ran Handle.
        Assert.Equal(2, result.Value);
    }

    [Fact]
    public async Task ISender_SendAsync_AllOverloads_ForwardToSend()
    {
        // Arrange
        ISender sender = new AsyncForwardingSender();
        var ct = TestContext.Current.CancellationToken;

        // Act & Assert - query overload
        var queryResult = await sender.SendAsync(new AsyncForwardingQuery(21), ct);
        Assert.Equal(42, queryResult.Value);

        // Act & Assert - value-returning command overload
        var commandResult = await sender.SendAsync(new AsyncForwardingCommand(21), ct);
        Assert.Equal(42, commandResult.Value);

        // Act & Assert - void command overload
        var voidResult = await sender.SendAsync<Outcome>(new AsyncForwardingVoidCommand(), ct);
        Assert.IsType<Success>(voidResult.Value);

        // Act & Assert - runtime-typed overload
        var objectResult = await sender.SendAsync((object)new AsyncForwardingQuery(21), ct);
        var typedObjectResult = Assert.IsType<Outcome<int, Failure>>(objectResult);
        Assert.Equal(42, typedObjectResult.Value);
    }

    [Fact]
    public async Task IPublisher_PublishAsync_BothOverloads_ForwardToPublish()
    {
        // Arrange
        var strategies = new List<PublishStrategy>();
        IPublisher publisher = new AsyncForwardingPublisher(strategies);
        var ct = TestContext.Current.CancellationToken;

        // Act
        await publisher.PublishAsync(new AsyncForwardingNotification("default-strategy"), ct);
        await publisher.PublishAsync(new AsyncForwardingNotification("explicit-strategy"), PublishStrategy.Parallel, ct);

        // Assert - only Publish (not PublishAsync) was implemented, so both calls had to forward through it.
        Assert.Equal([PublishStrategy.Sequential, PublishStrategy.Parallel], strategies);
    }

    [Fact]
    public async Task IHandlerWrapper_HandleAsync_ForwardsToHandle()
    {
        // Arrange
        IHandlerWrapper<Outcome<int, Failure>> wrapper = new AsyncForwardingHandlerWrapper();
        var services = new ServiceCollection().BuildServiceProvider();

        // Act
        var result = await wrapper.HandleAsync(new object(), services, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public async Task IVoidHandlerWrapper_HandleAsync_ForwardsToHandle()
    {
        // Arrange
        IVoidHandlerWrapper<Outcome> wrapper = new AsyncForwardingVoidHandlerWrapper();
        var services = new ServiceCollection().BuildServiceProvider();

        // Act
        var result = await wrapper.HandleAsync(new object(), services, TestContext.Current.CancellationToken);

        // Assert
        Assert.IsType<Success>(result.Value);
    }
}

public record AsyncForwardingCommand(int Value) : ICommand<Outcome<int, Failure>>;

/// <summary>Implements only <see cref="ICommandHandler{TCommand,TResponse}.Handle"/> - never the new member.</summary>
public class AsyncForwardingCommandHandler : ICommandHandler<AsyncForwardingCommand, Outcome<int, Failure>>
{
    public Task<Outcome<int, Failure>> Handle(AsyncForwardingCommand command, CancellationToken ct) =>
        Task.FromResult(new Outcome<int, Failure>(command.Value * 2));
}

public record AsyncForwardingVoidCommand : ICommand;

public record AsyncForwardingQuery(int Value) : IQuery<Outcome<int, Failure>>;

/// <summary>Implements only <see cref="IQueryHandler{TQuery,TResponse}.Handle"/> - never the new member.</summary>
public class AsyncForwardingQueryHandler : IQueryHandler<AsyncForwardingQuery, Outcome<int, Failure>>
{
    public Task<Outcome<int, Failure>> Handle(AsyncForwardingQuery query, CancellationToken ct) =>
        Task.FromResult(new Outcome<int, Failure>(query.Value * 2));
}

public record AsyncForwardingNotification(string Message) : INotification;

/// <summary>Implements only <see cref="INotificationHandler{TNotification}.Handle"/> - never the new member.</summary>
public class AsyncForwardingNotificationHandler(List<string> received) : INotificationHandler<AsyncForwardingNotification>
{
    public Task Handle(AsyncForwardingNotification notification, CancellationToken ct)
    {
        received.Add(notification.Message);
        return Task.CompletedTask;
    }
}

public record AsyncForwardingRequest(int Value);

/// <summary>Implements only <see cref="IPipelineBehavior{TRequest,TResponse}.Handle"/> - never the new member.</summary>
public class AsyncForwardingBehavior : IPipelineBehavior<AsyncForwardingRequest, Outcome<int, Failure>>
{
    public async Task<Outcome<int, Failure>> Handle(
        AsyncForwardingRequest request,
        Func<Task<Outcome<int, Failure>>> next,
        CancellationToken ct
    )
    {
        var result = await next().ConfigureAwait(false);
        return new Outcome<int, Failure>((int)result.Value * 2);
    }
}

/// <summary>Implements only the four old <c>Send</c> overloads - never any <c>SendAsync</c> overload.</summary>
public class AsyncForwardingSender : ISender
{
    public Task<TResponse> Send<TResponse>(IQuery<TResponse> query, CancellationToken ct = default)
        where TResponse : IOutcome => Task.FromResult((TResponse)(object)new Outcome<int, Failure>(42));

    public Task<TResponse> Send<TResponse>(ICommand<TResponse> command, CancellationToken ct = default)
        where TResponse : IOutcome => Task.FromResult((TResponse)(object)new Outcome<int, Failure>(42));

    public Task<TResult> Send<TResult>(ICommand command, CancellationToken ct = default)
        where TResult : IOutcome => Task.FromResult((TResult)(object)new Outcome(default(Success)));

    public Task<IOutcome> Send(object request, CancellationToken ct = default) =>
        Task.FromResult<IOutcome>(new Outcome<int, Failure>(42));
}

/// <summary>Implements only the two old <c>Publish</c> overloads - never either <c>PublishAsync</c> overload.</summary>
public class AsyncForwardingPublisher(List<PublishStrategy> strategies) : IPublisher
{
    public Task Publish<TNotification>(TNotification notification, CancellationToken ct = default)
        where TNotification : INotification => this.Publish(notification, PublishStrategy.Sequential, ct);

    public Task Publish<TNotification>(TNotification notification, PublishStrategy strategy, CancellationToken ct = default)
        where TNotification : INotification
    {
        strategies.Add(strategy);
        return Task.CompletedTask;
    }
}

/// <summary>Implements only <see cref="IHandlerWrapper{TResponse}.Handle"/> - never the new member.</summary>
internal sealed class AsyncForwardingHandlerWrapper : IHandlerWrapper<Outcome<int, Failure>>
{
    public Task<Outcome<int, Failure>> Handle(
        object request,
        IServiceProvider services,
        CancellationToken ct,
        Activity? activity = null
    ) => Task.FromResult(new Outcome<int, Failure>(42));
}

/// <summary>Implements only <see cref="IVoidHandlerWrapper{TResult}.Handle"/> - never the new member.</summary>
internal sealed class AsyncForwardingVoidHandlerWrapper : IVoidHandlerWrapper<Outcome>
{
    public Task<Outcome> Handle(object request, IServiceProvider services, CancellationToken ct, Activity? activity = null) =>
        Task.FromResult(new Outcome(default(Success)));
}
