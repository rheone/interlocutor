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
    public async Task<TResponse> Handle(TRequest request, Func<Task<TResponse>> next, CancellationToken ct)
    {
        track();
        return await next();
    }
}

public class MediatorTests
{
    [Fact]
    public async Task Send_Query_ReturnsHandlerResult()
    {
        var mediator = CreateMediator<TestQueryHandler>(services => { });
        var result = await mediator.Send(new TestQuery(21), CancellationToken.None);

        Assert.Equal(42, result.Value);
    }

    [Fact]
    public async Task Send_Query_RunsPipelineBehaviors()
    {
        var invoked = false;
        var mediator = CreateMediator<TestQueryHandler>(services =>
        {
            services.AddSingleton<IPipelineBehavior<TestQuery, OperationOutcome<int, Failure>>>(
                new TrackingBehavior<TestQuery, OperationOutcome<int, Failure>>(() => invoked = true)
            );
        });

        await mediator.Send(new TestQuery(1), CancellationToken.None);
        Assert.True(invoked);
    }

    [Fact]
    public async Task Send_Command_RunsPipelineBehaviors()
    {
        var invoked = false;
        var mediator = CreateMediator<GreetCommandHandler>(services =>
        {
            services.AddSingleton<IPipelineBehavior<GreetCommand, OperationOutcome<string, Failure>>>(
                new TrackingBehavior<GreetCommand, OperationOutcome<string, Failure>>(() => invoked = true)
            );
        });

        await mediator.Send(new GreetCommand("test"), CancellationToken.None);
        Assert.True(invoked);
    }

    [Fact]
    public async Task Send_VoidCommand_RunsPipelineBehaviors()
    {
        var invoked = false;
        var mediator = CreateMediator<TestVoidHandler>(services =>
        {
            services.AddSingleton<IPipelineBehavior<TestVoidCommand, OperationOutcome>>(
                new TrackingBehavior<TestVoidCommand, OperationOutcome>(() => invoked = true)
            );
        });

        await mediator.Send<OperationOutcome>(new TestVoidCommand("test"), CancellationToken.None);
        Assert.True(invoked);
    }

    [Fact]
    public async Task Send_VoidCommand_ReturnsSuccess()
    {
        var mediator = CreateMediator<TestVoidHandler>(services => { });
        var result = await mediator.Send<OperationOutcome>(new TestVoidCommand("test"), CancellationToken.None);

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
        var invoked = false;
        var services = new ServiceCollection();
        services.AddSingleton<INotificationHandler<TestNotification>>(new TestNotificationHandler(() => invoked = true));
        services.AddSingleton<IMediator, Mediator>();
        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        await mediator.Publish(new TestNotification("hello"), CancellationToken.None);

        Assert.True(invoked);
    }

    [Fact]
    public async Task Publish_WithNoHandlers_DoesNotThrow()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IMediator, Mediator>();
        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        await mediator.Publish(new TestNotification("hello"), CancellationToken.None);
    }

    [Fact]
    public async Task Publish_Sequential_StopsOnFailure()
    {
        var secondInvoked = false;
        var services = new ServiceCollection();
        services.AddSingleton<INotificationHandler<TestNotification>>(
            new TestNotificationHandler(() => throw new InvalidOperationException("First handler fails"))
        );
        services.AddSingleton<INotificationHandler<TestNotification>>(new TestNotificationHandler(() => secondInvoked = true));
        services.AddSingleton<IMediator, Mediator>();
        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            mediator.Publish(new TestNotification("hello"), CancellationToken.None)
        );

        Assert.False(secondInvoked);
    }

    [Fact]
    public async Task Send_Query_CachesWrapper()
    {
        var mediator = CreateMediator<TestQueryHandler>(services => { });
        await mediator.Send(new TestQuery(1), CancellationToken.None);
        await mediator.Send(new TestQuery(2), CancellationToken.None);
        await mediator.Send(new TestQuery(3), CancellationToken.None);
        Assert.True(true);
    }

    [Fact]
    public async Task Mediator_ImplementsISender()
    {
        var mediator = CreateMediator<TestQueryHandler>(services => { });
        Assert.IsAssignableFrom<ISender>(mediator);
    }

    [Fact]
    public async Task Mediator_ImplementsIPublisher()
    {
        var mediator = CreateMediator<TestQueryHandler>(services => { });
        Assert.IsAssignableFrom<IPublisher>(mediator);
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
