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
        // Act
        var registrations = HandlerDiscovery.Discover([typeof(HandlerDiscoveryTests).Assembly]);

        // Assert
        var registration = Assert.Single(registrations, r => r.ImplementationType == typeof(SampleQueryHandler));

        Assert.Equal(typeof(IQueryHandler<SampleQuery, Outcome>), registration.ServiceType);
        Assert.Equal(RegistrationLifetime.Scoped, registration.Lifetime);
        Assert.Equal(RegistrationKind.Handler, registration.Kind);
        Assert.Equal(RegistrationProvenance.Scanned, registration.Provenance);
    }

    [Fact]
    public void Discover_WithPerAssemblyLifetime_AppliesItToEveryMatchFromThatAssembly()
    {
        // Act
        var registrations = HandlerDiscovery.Discover([
            (typeof(HandlerDiscoveryTests).Assembly, RegistrationLifetime.Singleton),
        ]);

        // Assert
        var registration = Assert.Single(registrations, r => r.ImplementationType == typeof(SampleQueryHandler));

        Assert.Equal(RegistrationLifetime.Singleton, registration.Lifetime);
        Assert.Equal(RegistrationProvenance.Scanned, registration.Provenance);
    }

    [Fact]
    public void Discover_FindsBareCommandHandler_AndNotificationHandler()
    {
        // Act
        var registrations = HandlerDiscovery.Discover([typeof(HandlerDiscoveryTests).Assembly]);

        // Assert
        Assert.Contains(
            registrations,
            r =>
                r.ImplementationType == typeof(SampleBareCommandHandler)
                && r.ServiceType == typeof(IBareCommandHandler<SampleCommand, Outcome>)
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
        // Act
        var registrations = HandlerDiscovery.Discover([typeof(HandlerDiscoveryTests).Assembly]);

        // Assert
        Assert.DoesNotContain(registrations, r => r.ImplementationType == typeof(NotAHandler));
    }

    [Fact]
    public void Discover_FindsCommandHandler_AsScopedHandlerRegistration()
    {
        // Act
        var registrations = HandlerDiscovery.Discover([typeof(HandlerDiscoveryTests).Assembly]);

        // Assert
        var registration = Assert.Single(registrations, r => r.ImplementationType == typeof(SampleCommandHandler));

        Assert.Equal(typeof(ICommandHandler<SampleCommandWithResponse, Outcome>), registration.ServiceType);
        Assert.Equal(RegistrationLifetime.Scoped, registration.Lifetime);
        Assert.Equal(RegistrationKind.Handler, registration.Kind);
    }

    [Fact]
    public void Discover_FindsTypeImplementingMultipleHandlerContracts_ForEachContract()
    {
        // Act
        var registrations = HandlerDiscovery.Discover([typeof(HandlerDiscoveryTests).Assembly]);

        // Assert
        var forType = registrations.Where(r => r.ImplementationType == typeof(MultiContractHandler)).ToList();

        Assert.Equal(2, forType.Count);

        Assert.Contains(
            forType,
            r => r.ServiceType == typeof(IQueryHandler<MultiContractQuery, Outcome>) && r.Kind == RegistrationKind.Handler
        );

        Assert.Contains(
            forType,
            r => r.ServiceType == typeof(INotificationHandler<MultiContractNotification>) && r.Kind == RegistrationKind.Handler
        );
    }

    [Fact]
    public void Discover_FindsEveryNotificationHandler_WhenMultipleHandlersShareOneNotificationType()
    {
        // Act
        var registrations = HandlerDiscovery.Discover([typeof(HandlerDiscoveryTests).Assembly]);

        // Assert
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
        // Act
        // HandlerDiscovery.Discover currently scans every concrete (non-abstract) class, including open
        // generic class definitions. Type.GetInterfaces() on an open generic class still resolves each
        // implemented interface's generic type definition, so an open generic handler class IS discovered
        // here - its ServiceType is the still-open generic interface (e.g. IQueryHandler<GenericQuery<T>, ...>).
        // This is current, actual behavior, not endorsed generic-handler support: closing an open generic
        // handler against concrete request types (ticket di-builder-and-discovery/11) is unimplemented, so a
        // container attempting to register this ServiceType directly would fail - callers of Discover must
        // filter out open generics themselves until that ticket lands.
        var registrations = HandlerDiscovery.Discover([typeof(HandlerDiscoveryTests).Assembly]);

        // Assert
        var registration = Assert.Single(registrations, r => r.ImplementationType == typeof(OpenGenericQueryHandler<>));

        Assert.True(registration.ImplementationType.IsGenericTypeDefinition);
        Assert.True(registration.ServiceType.ContainsGenericParameters);
        Assert.Equal(typeof(IQueryHandler<,>), registration.ServiceType.GetGenericTypeDefinition());
        Assert.Equal(RegistrationLifetime.Scoped, registration.Lifetime);
        Assert.Equal(RegistrationKind.Handler, registration.Kind);
    }

    private sealed record SampleQuery : IQuery<Outcome>;

    private sealed record SampleCommand : ICommand;

    private sealed record SampleCommandWithResponse : ICommand<Outcome>;

    private sealed record SampleNotification : INotification;

    private sealed record MultiContractQuery : IQuery<Outcome>;

    private sealed record MultiContractNotification : INotification;

    private sealed record SharedNotification : INotification;

    // T is deliberately left unconstrained by any member - this record exists to prove open-generic
    // handler discovery (a handler registered for GenericQuery<> closes over whatever T the caller uses).
#pragma warning disable S2326
    private sealed record GenericQuery<T> : IQuery<Outcome>;
#pragma warning restore S2326

    // Deliberately empty: a negative-case marker that implements no handler interface, not a data holder.
#pragma warning disable S2094
    private sealed class NotAHandler;
#pragma warning restore S2094

    private sealed class SampleQueryHandler : IQueryHandler<SampleQuery, Outcome>
    {
        public Task<Outcome> HandleAsync(SampleQuery query, CancellationToken ct) => Task.FromResult(default(Outcome));
    }

    private sealed class SampleBareCommandHandler : IBareCommandHandler<SampleCommand, Outcome>
    {
        public Task<Outcome> HandleAsync(SampleCommand command, CancellationToken ct) => Task.FromResult(default(Outcome));
    }

    private sealed class SampleCommandHandler : ICommandHandler<SampleCommandWithResponse, Outcome>
    {
        public Task<Outcome> HandleAsync(SampleCommandWithResponse command, CancellationToken ct) =>
            Task.FromResult(default(Outcome));
    }

    private sealed class SampleNotificationHandler : INotificationHandler<SampleNotification>
    {
        public Task HandleAsync(SampleNotification notification, CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class MultiContractHandler
        : IQueryHandler<MultiContractQuery, Outcome>,
            INotificationHandler<MultiContractNotification>
    {
        public Task<Outcome> HandleAsync(MultiContractQuery query, CancellationToken ct) => Task.FromResult(default(Outcome));

        public Task HandleAsync(MultiContractNotification notification, CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class FirstSharedNotificationHandler : INotificationHandler<SharedNotification>
    {
        public Task HandleAsync(SharedNotification notification, CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class SecondSharedNotificationHandler : INotificationHandler<SharedNotification>
    {
        public Task HandleAsync(SharedNotification notification, CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class OpenGenericQueryHandler<T> : IQueryHandler<GenericQuery<T>, Outcome>
    {
        public Task<Outcome> HandleAsync(GenericQuery<T> query, CancellationToken ct) => Task.FromResult(default(Outcome));
    }
}
