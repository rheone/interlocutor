namespace Interlocutor.DependencyInjection.Abstractions.Tests;

using Interlocutor.Abstractions.Commands;
using Interlocutor.Abstractions.Notifications;
using Interlocutor.Abstractions.Queries;
using Interlocutor.CommonKernel;

public class HandlerDiscoveryTests
{
    [Fact]
    public void Discover_FindsQueryHandler_AsScopedHandlerRegistration()
    {
        var registrations = HandlerDiscovery.Discover([typeof(HandlerDiscoveryTests).Assembly]);

        var registration = Assert.Single(registrations, r => r.ImplementationType == typeof(SampleQueryHandler));

        Assert.Equal(typeof(IQueryHandler<SampleQuery, OperationOutcome>), registration.ServiceType);
        Assert.Equal(RegistrationLifetime.Scoped, registration.Lifetime);
        Assert.Equal(RegistrationKind.Handler, registration.Kind);
    }

    [Fact]
    public void Discover_FindsVoidCommandHandler_AndNotificationHandler()
    {
        var registrations = HandlerDiscovery.Discover([typeof(HandlerDiscoveryTests).Assembly]);

        Assert.Contains(
            registrations,
            r =>
                r.ImplementationType == typeof(SampleVoidCommandHandler)
                && r.ServiceType == typeof(IVoidCommandHandler<SampleCommand, OperationOutcome>)
                && r.Kind == RegistrationKind.Handler
        );

        Assert.Contains(
            registrations,
            r =>
                r.ImplementationType == typeof(SampleNotificationHandler)
                && r.ServiceType == typeof(INotificationHandler<SampleNotification>)
                && r.Kind == RegistrationKind.Handler
        );
    }

    [Fact]
    public void Discover_IgnoresTypesThatAreNotHandlers()
    {
        var registrations = HandlerDiscovery.Discover([typeof(HandlerDiscoveryTests).Assembly]);

        Assert.DoesNotContain(registrations, r => r.ImplementationType == typeof(NotAHandler));
    }

    [Fact]
    public void Discover_FindsCommandHandler_AsScopedHandlerRegistration()
    {
        var registrations = HandlerDiscovery.Discover([typeof(HandlerDiscoveryTests).Assembly]);

        var registration = Assert.Single(registrations, r => r.ImplementationType == typeof(SampleCommandHandler));

        Assert.Equal(typeof(ICommandHandler<SampleCommandWithResponse, OperationOutcome>), registration.ServiceType);
        Assert.Equal(RegistrationLifetime.Scoped, registration.Lifetime);
        Assert.Equal(RegistrationKind.Handler, registration.Kind);
    }

    [Fact]
    public void Discover_FindsTypeImplementingMultipleHandlerContracts_ForEachContract()
    {
        var registrations = HandlerDiscovery.Discover([typeof(HandlerDiscoveryTests).Assembly]);

        var forType = registrations.Where(r => r.ImplementationType == typeof(MultiContractHandler)).ToList();

        Assert.Equal(2, forType.Count);

        Assert.Contains(
            forType,
            r =>
                r.ServiceType == typeof(IQueryHandler<MultiContractQuery, OperationOutcome>)
                && r.Kind == RegistrationKind.Handler
        );

        Assert.Contains(
            forType,
            r => r.ServiceType == typeof(INotificationHandler<MultiContractNotification>) && r.Kind == RegistrationKind.Handler
        );
    }

    [Fact]
    public void Discover_FindsEveryNotificationHandler_WhenMultipleHandlersShareOneNotificationType()
    {
        var registrations = HandlerDiscovery.Discover([typeof(HandlerDiscoveryTests).Assembly]);

        var handlersForNotification = registrations
            .Where(r => r.ServiceType == typeof(INotificationHandler<SharedNotification>))
            .ToList();

        Assert.Equal(2, handlersForNotification.Count);
        Assert.Contains(handlersForNotification, r => r.ImplementationType == typeof(FirstSharedNotificationHandler));
        Assert.Contains(handlersForNotification, r => r.ImplementationType == typeof(SecondSharedNotificationHandler));
    }

    [Fact]
    public void Discover_FindsOpenGenericHandlerClass_ClosedOverItsOwnTypeParameter()
    {
        // HandlerDiscovery.Discover currently scans every concrete (non-abstract) class, including open
        // generic class definitions. Type.GetInterfaces() on an open generic class still resolves each
        // implemented interface's generic type definition, so an open generic handler class IS discovered
        // here - its ServiceType is the still-open generic interface (e.g. IQueryHandler<GenericQuery<T>, ...>).
        // This is current, actual behavior, not endorsed generic-handler support: closing an open generic
        // handler against concrete request types (ticket di-builder-and-discovery/11) is unimplemented, so a
        // container attempting to register this ServiceType directly would fail - callers of Discover must
        // filter out open generics themselves until that ticket lands.
        var registrations = HandlerDiscovery.Discover([typeof(HandlerDiscoveryTests).Assembly]);

        var registration = Assert.Single(registrations, r => r.ImplementationType == typeof(OpenGenericQueryHandler<>));

        Assert.True(registration.ImplementationType.IsGenericTypeDefinition);
        Assert.True(registration.ServiceType.ContainsGenericParameters);
        Assert.Equal(typeof(IQueryHandler<,>), registration.ServiceType.GetGenericTypeDefinition());
        Assert.Equal(RegistrationLifetime.Scoped, registration.Lifetime);
        Assert.Equal(RegistrationKind.Handler, registration.Kind);
    }

    private sealed record SampleQuery : IQuery<OperationOutcome>;

    private sealed record SampleCommand : ICommand;

    private sealed record SampleCommandWithResponse : ICommand<OperationOutcome>;

    private sealed record SampleNotification : INotification;

    private sealed record MultiContractQuery : IQuery<OperationOutcome>;

    private sealed record MultiContractNotification : INotification;

    private sealed record SharedNotification : INotification;

    private sealed record GenericQuery<T> : IQuery<OperationOutcome>;

    private sealed class NotAHandler;

    private sealed class SampleQueryHandler : IQueryHandler<SampleQuery, OperationOutcome>
    {
        public Task<OperationOutcome> Handle(SampleQuery query, CancellationToken ct) =>
            Task.FromResult(default(OperationOutcome));
    }

    private sealed class SampleVoidCommandHandler : IVoidCommandHandler<SampleCommand, OperationOutcome>
    {
        public Task<OperationOutcome> Handle(SampleCommand command, CancellationToken ct) =>
            Task.FromResult(default(OperationOutcome));
    }

    private sealed class SampleCommandHandler : ICommandHandler<SampleCommandWithResponse, OperationOutcome>
    {
        public Task<OperationOutcome> Handle(SampleCommandWithResponse command, CancellationToken ct) =>
            Task.FromResult(default(OperationOutcome));
    }

    private sealed class SampleNotificationHandler : INotificationHandler<SampleNotification>
    {
        public Task Handle(SampleNotification notification, CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class MultiContractHandler
        : IQueryHandler<MultiContractQuery, OperationOutcome>,
            INotificationHandler<MultiContractNotification>
    {
        public Task<OperationOutcome> Handle(MultiContractQuery query, CancellationToken ct) =>
            Task.FromResult(default(OperationOutcome));

        public Task Handle(MultiContractNotification notification, CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class FirstSharedNotificationHandler : INotificationHandler<SharedNotification>
    {
        public Task Handle(SharedNotification notification, CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class SecondSharedNotificationHandler : INotificationHandler<SharedNotification>
    {
        public Task Handle(SharedNotification notification, CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class OpenGenericQueryHandler<T> : IQueryHandler<GenericQuery<T>, OperationOutcome>
    {
        public Task<OperationOutcome> Handle(GenericQuery<T> query, CancellationToken ct) =>
            Task.FromResult(default(OperationOutcome));
    }
}
