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

    private sealed record SampleQuery : IQuery<OperationOutcome>;

    private sealed record SampleCommand : ICommand;

    private sealed record SampleNotification : INotification;

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

    private sealed class SampleNotificationHandler : INotificationHandler<SampleNotification>
    {
        public Task Handle(SampleNotification notification, CancellationToken ct) => Task.CompletedTask;
    }
}
