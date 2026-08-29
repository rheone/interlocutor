namespace Interlocutor.DependencyInjection.Microsoft.Tests;

using global::Microsoft.Extensions.DependencyInjection;
using Interlocutor.Abstractions.Core;
using Interlocutor.Abstractions.Notifications;
using Interlocutor.DependencyInjection.Abstractions;

/// <summary>
/// Proves ticket 14's contravariant fan-out against a real Microsoft DI <see cref="IServiceProvider"/> built
/// through <c>AddInterlocutor</c>/<c>ScanAssembly</c>, mirroring the Autofac counterpart of this test class -
/// see that class's remarks for why both containers need their own proof.
/// </summary>
public class NotificationFanOutIntegrationTests
{
    [Fact]
    public async Task Publish_DerivedEvent_InvokesHandlerRegisteredForBaseType()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddScoped<InvocationRecorder>();
        services.AddInterlocutor(builder => builder.ScanAssembly(typeof(NotificationFanOutIntegrationTests).Assembly));

        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        // Act
        await mediator.Publish(new OrderCreated(1), TestContext.Current.CancellationToken);

        // Assert
        Assert.True(scope.ServiceProvider.GetRequiredService<InvocationRecorder>().Invoked);
    }

    /// <summary>Records whether <see cref="DomainEventLogger"/> ran, without relying on shared static state.</summary>
    private sealed class InvocationRecorder
    {
        public bool Invoked { get; set; }
    }

    private record DomainEvent(int AggregateId) : INotification;

    private sealed record OrderCreated(int AggregateId) : DomainEvent(AggregateId);

    // Resolved only via AddInterlocutor()'s assembly-scanning DI registration - never new'd or
    // typeof'd directly, so this constructor is invisible to static analysis.
#pragma warning disable S1144
    private sealed class DomainEventLogger(InvocationRecorder recorder) : INotificationHandler<DomainEvent>
    {
        public Task Handle(DomainEvent notification, CancellationToken ct)
        {
            recorder.Invoked = true;
            return Task.CompletedTask;
        }
    }
#pragma warning restore S1144
}
