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

public record TestQuery(int Value) : IQuery<OperationOutcome<int, Failure>>;

public class TestQueryHandler : IQueryHandler<TestQuery, OperationOutcome<int, Failure>>
{
    public Task<OperationOutcome<int, Failure>> Handle(TestQuery query, CancellationToken ct) =>
        Task.FromResult(new OperationOutcome<int, Failure>(query.Value * 2));
}

public record GreetCommand(string PersonName) : ICommand<OperationOutcome<string, Failure>>;

public class GreetCommandHandler : ICommandHandler<GreetCommand, OperationOutcome<string, Failure>>
{
    public Task<OperationOutcome<string, Failure>> Handle(GreetCommand command, CancellationToken ct) =>
        Task.FromResult(new OperationOutcome<string, Failure>($"Hello {command.PersonName}"));
}

public record TestVoidCommand(string Value) : ICommand;

public class TestVoidHandler : IVoidCommandHandler<TestVoidCommand, OperationOutcome>
{
    public Task<OperationOutcome> Handle(TestVoidCommand command, CancellationToken ct) =>
        Task.FromResult(new OperationOutcome(default(Success)));
}

public record TestNotification(string Message) : INotification;

public class TrackingBehavior<TRequest, TResponse>(Action track) : IPipelineBehavior<TRequest, TResponse>
{
    public Task<TResponse> Handle(TRequest request, Func<Task<TResponse>> next, CancellationToken ct)
    {
        track();
        return next();
    }
}

public class MediatorTests
{
    [Fact]
    public async Task Send_Query_ReturnsHandlerResult()
    {
        // Arrange
        var mediator = CreateMediator<TestQueryHandler>(_ => { });

        // Act
        var result = await mediator.Send(new TestQuery(21), CancellationToken.None);

        // Assert
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public async Task Send_Query_RunsPipelineBehaviors()
    {
        // Arrange
        var invoked = false;
        var mediator = CreateMediator<TestQueryHandler>(services =>
        {
            services.AddSingleton<IPipelineBehavior<TestQuery, OperationOutcome<int, Failure>>>(
                new TrackingBehavior<TestQuery, OperationOutcome<int, Failure>>(() => invoked = true)
            );
        });

        // Act
        await mediator.Send(new TestQuery(1), CancellationToken.None);

        // Assert
        Assert.True(invoked);
    }

    [Fact]
    public async Task Send_Command_RunsPipelineBehaviors()
    {
        // Arrange
        var invoked = false;
        var mediator = CreateMediator<GreetCommandHandler>(services =>
        {
            services.AddSingleton<IPipelineBehavior<GreetCommand, OperationOutcome<string, Failure>>>(
                new TrackingBehavior<GreetCommand, OperationOutcome<string, Failure>>(() => invoked = true)
            );
        });

        // Act
        await mediator.Send(new GreetCommand("test"), CancellationToken.None);

        // Assert
        Assert.True(invoked);
    }

    [Fact]
    public async Task Send_VoidCommand_RunsPipelineBehaviors()
    {
        // Arrange
        var invoked = false;
        var mediator = CreateMediator<TestVoidHandler>(services =>
        {
            services.AddSingleton<IPipelineBehavior<TestVoidCommand, OperationOutcome>>(
                new TrackingBehavior<TestVoidCommand, OperationOutcome>(() => invoked = true)
            );
        });

        // Act
        await mediator.Send<OperationOutcome>(new TestVoidCommand("test"), CancellationToken.None);

        // Assert
        Assert.True(invoked);
    }

    [Fact]
    public async Task Send_VoidCommand_ReturnsSuccess()
    {
        // Arrange
        var mediator = CreateMediator<TestVoidHandler>(_ => { });

        // Act
        var result = await mediator.Send<OperationOutcome>(new TestVoidCommand("test"), CancellationToken.None);

        // Assert
        var isSuccess = result.Value switch
        {
            Success => true,
            Failure => false,
            _ => throw new UnreachableException(),
        };
        Assert.True(isSuccess);
    }

    [Fact]
    public async Task Publish_Sequential_InvokesHandlers()
    {
        // Arrange
        var invoked = false;
        var services = new ServiceCollection();
        services.AddSingleton<INotificationHandler<TestNotification>>(new TestNotificationHandler(() => invoked = true));
        services.AddSingleton<IMediator, Mediator>();
        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        // Act
        await mediator.Publish(new TestNotification("hello"), CancellationToken.None);

        // Assert
        Assert.True(invoked);
    }

    [Fact]
    public Task Publish_WithNoHandlers_DoesNotThrow()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IMediator, Mediator>();
        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        // Act & Assert - no handlers registered for TestNotification; publishing must not throw.
        return mediator.Publish(new TestNotification("hello"), CancellationToken.None);
    }

    [Fact]
    public async Task Publish_Sequential_StopsOnFailure()
    {
        // Arrange
        var secondInvoked = false;
        var services = new ServiceCollection();
        services.AddSingleton<INotificationHandler<TestNotification>>(
            new TestNotificationHandler(() => throw new InvalidOperationException("First handler fails"))
        );
        services.AddSingleton<INotificationHandler<TestNotification>>(new TestNotificationHandler(() => secondInvoked = true));
        services.AddSingleton<IMediator, Mediator>();
        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        // Act
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            mediator.Publish(new TestNotification("hello"), CancellationToken.None)
        );

        // Assert
        Assert.False(secondInvoked);
    }

    [Fact]
    public async Task Send_Query_ReusedAcrossMultipleDispatches_ReturnsCorrectResultEachTime()
    {
        // Arrange - the wrapper Mediator builds per closed request type is cached internally; sending the
        // same query type repeatedly must keep returning a result derived from that specific dispatch's
        // input, not a stale or shared one.
        var mediator = CreateMediator<TestQueryHandler>(_ => { });

        // Act
        var first = await mediator.Send(new TestQuery(1), CancellationToken.None);
        var second = await mediator.Send(new TestQuery(2), CancellationToken.None);
        var third = await mediator.Send(new TestQuery(3), CancellationToken.None);

        // Assert
        Assert.Equal(2, first.Value);
        Assert.Equal(4, second.Value);
        Assert.Equal(6, third.Value);
    }

    [Fact]
    public async Task Mediator_ImplementsISender()
    {
        // Arrange
        var mediator = CreateMediator<TestQueryHandler>(_ => { });

        // Act & Assert
        Assert.IsType<ISender>(mediator, exactMatch: false);
    }

    [Fact]
    public async Task Mediator_ImplementsIPublisher()
    {
        // Arrange
        var mediator = CreateMediator<TestQueryHandler>(_ => { });

        // Act & Assert
        Assert.IsType<IPublisher>(mediator, exactMatch: false);
    }

    private static IMediator CreateMediator<THandler>(Action<ServiceCollection> configure)
    {
        var services = new ServiceCollection();

        if (typeof(THandler) == typeof(TestQueryHandler))
        {
            services.AddSingleton<IQueryHandler<TestQuery, OperationOutcome<int, Failure>>, TestQueryHandler>();
        }
        else if (typeof(THandler) == typeof(GreetCommandHandler))
        {
            services.AddSingleton<ICommandHandler<GreetCommand, OperationOutcome<string, Failure>>, GreetCommandHandler>();
        }
        else if (typeof(THandler) == typeof(TestVoidHandler))
        {
            services.AddSingleton<IVoidCommandHandler<TestVoidCommand, OperationOutcome>, TestVoidHandler>();
        }

        configure(services);
        services.AddSingleton<IMediator, Mediator>();
        var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IMediator>();
    }
}

public class TestNotificationHandler(Action action) : INotificationHandler<TestNotification>
{
    public Task Handle(TestNotification notification, CancellationToken ct)
    {
        action();
        return Task.CompletedTask;
    }
}
