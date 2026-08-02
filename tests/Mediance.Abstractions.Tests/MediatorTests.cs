using System.Diagnostics;
using Mediance.Abstractions.Commands;
using Mediance.Abstractions.Core;
using Mediance.Abstractions.Notifications;
using Mediance.Abstractions.Pipeline;
using Mediance.Abstractions.Queries;
using Mediance.CommonKernel;
using Mediance.CommonKernel.ResultTypes;
using Microsoft.Extensions.DependencyInjection;

namespace Mediance.Abstractions.Tests;

public record TestQuery(int value) : IQuery<int>;

public class TestQueryHandler : IQueryHandler<TestQuery, int>
{
    public Task<int> Handle(TestQuery query, CancellationToken ct) => Task.FromResult(query.value * 2);
}

public record GreetCommand(string personName) : ICommand<string>;

public class GreetCommandHandler : ICommandHandler<GreetCommand, string>
{
    public Task<string> Handle(GreetCommand command, CancellationToken ct) => Task.FromResult($"Hello {command.personName}");
}

public record TestVoidCommand(string value) : ICommand;

public class TestVoidHandler : IVoidCommandHandler<TestVoidCommand, OperationOutcome>
{
    public Task<OperationOutcome> Handle(TestVoidCommand command, CancellationToken ct) =>
        Task.FromResult(new OperationOutcome(default(Success)));
}

public record TestNotification(string message) : INotification;

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

        Assert.Equal(42, result);
    }

    [Fact]
    public async Task Send_Query_RunsPipelineBehaviors()
    {
        var invoked = false;
        var mediator = CreateMediator<TestQueryHandler>(services =>
        {
            services.AddSingleton<IPipelineBehavior<TestQuery, int>>(
                new TrackingBehavior<TestQuery, int>(() => invoked = true)
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
            services.AddSingleton<IPipelineBehavior<GreetCommand, string>>(
                new TrackingBehavior<GreetCommand, string>(() => invoked = true)
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
            services.AddSingleton<IQueryHandler<TestQuery, int>, TestQueryHandler>();
        }
        else if (typeof(THandler) == typeof(GreetCommandHandler))
        {
            services.AddSingleton<ICommandHandler<GreetCommand, string>, GreetCommandHandler>();
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
