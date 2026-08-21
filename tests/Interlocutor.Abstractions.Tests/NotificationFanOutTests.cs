namespace Interlocutor.Abstractions.Tests;

using Interlocutor.Abstractions.Core;
using Interlocutor.Abstractions.Notifications;
using Microsoft.Extensions.DependencyInjection;

public record DomainEvent(int AggregateId) : INotification;

public record OrderCreated(int AggregateId) : DomainEvent(AggregateId);

public interface IAuditableEvent : INotification;

public record AuditedOrderCreated(int AggregateId) : DomainEvent(AggregateId), IAuditableEvent;

/// <summary>
/// Covers ticket 14's core gap: a handler registered against a base event type or interface must receive
/// derived events published as the derived type, and the publish-strategy overload must be reachable through
/// <see cref="IPublisher"/> itself, not only the concrete <see cref="Mediator"/>.
/// </summary>
public sealed class NotificationFanOutTests
{
    [Fact]
    public async Task Publish_DerivedEvent_InvokesHandlerRegisteredForBaseType()
    {
        var invoked = false;
        var services = new ServiceCollection();
        services.AddSingleton<INotificationHandler<DomainEvent>>(new RecordingHandler<DomainEvent>(() => invoked = true));
        services.AddSingleton<IMediator, Mediator>();
        var mediator = services.BuildServiceProvider().GetRequiredService<IMediator>();

        await mediator.Publish(new OrderCreated(1), CancellationToken.None);

        Assert.True(invoked);
    }

    [Fact]
    public async Task Publish_DerivedEvent_InvokesHandlersAtEveryLevel()
    {
        var concreteInvoked = false;
        var baseInvoked = false;
        var services = new ServiceCollection();
        services.AddSingleton<INotificationHandler<OrderCreated>>(
            new RecordingHandler<OrderCreated>(() => concreteInvoked = true)
        );
        services.AddSingleton<INotificationHandler<DomainEvent>>(new RecordingHandler<DomainEvent>(() => baseInvoked = true));
        services.AddSingleton<IMediator, Mediator>();
        var mediator = services.BuildServiceProvider().GetRequiredService<IMediator>();

        await mediator.Publish(new OrderCreated(1), CancellationToken.None);

        Assert.True(concreteInvoked);
        Assert.True(baseInvoked);
    }

    [Fact]
    public async Task Publish_Event_InvokesHandlerRegisteredForImplementedInterface()
    {
        var invoked = false;
        var services = new ServiceCollection();
        services.AddSingleton<INotificationHandler<IAuditableEvent>>(
            new RecordingHandler<IAuditableEvent>(() => invoked = true)
        );
        services.AddSingleton<IMediator, Mediator>();
        var mediator = services.BuildServiceProvider().GetRequiredService<IMediator>();

        await mediator.Publish(new AuditedOrderCreated(1), CancellationToken.None);

        Assert.True(invoked);
    }

    [Fact]
    public async Task Publish_Event_DoesNotInvokeHandlerForUnrelatedSiblingType()
    {
        var invoked = false;
        var services = new ServiceCollection();
        services.AddSingleton<INotificationHandler<OrderCreated>>(new RecordingHandler<OrderCreated>(() => invoked = true));
        services.AddSingleton<IMediator, Mediator>();
        var mediator = services.BuildServiceProvider().GetRequiredService<IMediator>();

        // AuditedOrderCreated is a sibling of OrderCreated (both derive from DomainEvent, neither from the
        // other) - a handler registered against OrderCreated specifically must not receive it.
        await mediator.Publish(new AuditedOrderCreated(1), CancellationToken.None);

        Assert.False(invoked);
    }

    [Fact]
    public async Task Publish_Event_InvokesCatchAllHandlerRegisteredForBaseNotificationInterface()
    {
        var invoked = false;
        var services = new ServiceCollection();
        services.AddSingleton<INotificationHandler<INotification>>(new RecordingHandler<INotification>(() => invoked = true));
        services.AddSingleton<IMediator, Mediator>();
        var mediator = services.BuildServiceProvider().GetRequiredService<IMediator>();

        await mediator.Publish(new OrderCreated(1), CancellationToken.None);

        Assert.True(invoked);
    }

    [Fact]
    public async Task IPublisher_ExposesStrategyOverload()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IMediator, Mediator>();
        IPublisher publisher = services.BuildServiceProvider().GetRequiredService<IMediator>();

        // Compiles and runs only if the strategy overload lives on IPublisher itself, not just on the
        // concrete Mediator class.
        await publisher.Publish(new OrderCreated(1), PublishStrategy.Parallel, CancellationToken.None);
    }

    [Fact]
    public async Task Publish_Sequential_OrdersHandlersByGeneratedRank()
    {
        var order = new List<string>();
        var services = new ServiceCollection();
        services.AddSingleton<INotificationHandler<DomainEvent>>(new RecordingHandler<DomainEvent>(() => order.Add("second")));
        services.AddSingleton<INotificationHandler<OrderCreated>>(new RecordingHandler<OrderCreated>(() => order.Add("first")));
        services.AddSingleton<IGeneratedNotificationHandlerOrder>(
            new FakeOrder(
                new Dictionary<Type, int>
                {
                    [typeof(RecordingHandler<OrderCreated>)] = 0,
                    [typeof(RecordingHandler<DomainEvent>)] = 1,
                }
            )
        );
        services.AddSingleton<IMediator, Mediator>();
        var mediator = services.BuildServiceProvider().GetRequiredService<IMediator>();

        await mediator.Publish(new OrderCreated(1), CancellationToken.None);

        Assert.Equal(["first", "second"], order);
    }

    [Fact]
    public async Task Publish_Sequential_LeavesUnrankedHandlersInResolutionOrder()
    {
        var order = new List<string>();
        var services = new ServiceCollection();
        services.AddSingleton<INotificationHandler<OrderCreated>>(new RecordingHandler<OrderCreated>(() => order.Add("a")));
        services.AddSingleton<INotificationHandler<OrderCreated>>(new RecordingHandler<OrderCreated>(() => order.Add("b")));
        services.AddSingleton<IGeneratedNotificationHandlerOrder>(new FakeOrder(new Dictionary<Type, int>()));
        services.AddSingleton<IMediator, Mediator>();
        var mediator = services.BuildServiceProvider().GetRequiredService<IMediator>();

        await mediator.Publish(new OrderCreated(1), CancellationToken.None);

        Assert.Equal(["a", "b"], order);
    }

    private sealed class FakeOrder(IReadOnlyDictionary<Type, int> ranks) : IGeneratedNotificationHandlerOrder
    {
        public int Rank(Type handlerType) => ranks.TryGetValue(handlerType, out var rank) ? rank : int.MaxValue;
    }
}

public class RecordingHandler<TNotification>(Action action) : INotificationHandler<TNotification>
    where TNotification : INotification
{
    public Task Handle(TNotification notification, CancellationToken ct)
    {
        action();
        return Task.CompletedTask;
    }
}
