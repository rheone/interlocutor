namespace Interlocutor.Testing.Tests;

using Interlocutor.Abstractions.Core;
using Interlocutor.Abstractions.Notifications;

/// <summary>Covers <see cref="FakePublisher"/>'s notification capture and fake-handler invocation.</summary>
public class FakePublisherTests
{
    [Fact]
    public async Task PublishAsync_WithNoRegisteredHandler_StillRecordsTheNotification()
    {
        // Arrange: no AddHandler call at all - a test asserting notification-driven code doesn't
        // always need to stand up a fake handler.
        var publisher = new FakePublisher();
        var notification = new UserCreated(1);

        // Act
        await publisher.PublishAsync(notification, TestContext.Current.CancellationToken);

        // Assert
        Assert.Same(notification, Assert.Single(publisher.PublishedNotifications));
    }

    [Fact]
    public async Task PublishAsync_InvokesARegisteredFakeHandler()
    {
        // Arrange
        var publisher = new FakePublisher();
        var handler = new RecordingUserCreatedHandler();
        publisher.AddHandler<UserCreated>(handler);
        var notification = new UserCreated(1);

        // Act
        await publisher.PublishAsync(notification, TestContext.Current.CancellationToken);

        // Assert: the handler's own observation is what the ticket calls "what those handlers observed" -
        // asserted on the fake handler the consumer supplied, not on the publisher.
        Assert.Same(notification, Assert.Single(handler.Handled));
    }

    [Fact]
    public async Task PublishAsync_InvokesEveryHandlerRegisteredForTheSameNotificationType()
    {
        // Arrange
        var publisher = new FakePublisher();
        var first = new RecordingUserCreatedHandler();
        var second = new RecordingUserCreatedHandler();
        publisher.AddHandler<UserCreated>(first);
        publisher.AddHandler<UserCreated>(second);
        var notification = new UserCreated(1);

        // Act
        await publisher.PublishAsync(notification, TestContext.Current.CancellationToken);

        // Assert
        Assert.Single(first.Handled);
        Assert.Single(second.Handled);
    }

    [Fact]
    public async Task PublishAsync_DoesNotInvokeAHandlerRegisteredForADifferentNotificationType()
    {
        // Arrange
        var publisher = new FakePublisher();
        var userHandler = new RecordingUserCreatedHandler();
        var orderHandler = new RecordingOrderShippedHandler();
        publisher.AddHandler<UserCreated>(userHandler);
        publisher.AddHandler<OrderShipped>(orderHandler);

        // Act
        await publisher.PublishAsync(new UserCreated(1), TestContext.Current.CancellationToken);

        // Assert
        Assert.Single(userHandler.Handled);
        Assert.Empty(orderHandler.Handled);
    }

    [Fact]
    public async Task PublishAsync_SequentialStrategy_StopsAtTheFirstThrowingHandler()
    {
        // Arrange
        var publisher = new FakePublisher();
        var throwing = new ThrowingUserCreatedHandler();
        var neverReached = new RecordingUserCreatedHandler();
        publisher.AddHandler<UserCreated>(throwing);
        publisher.AddHandler<UserCreated>(neverReached);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            publisher.PublishAsync(new UserCreated(1), PublishStrategy.Sequential, TestContext.Current.CancellationToken)
        );
        Assert.Empty(neverReached.Handled);
    }

    [Fact]
    public async Task PublishAsync_ParallelStrategy_InvokesEveryHandlerEvenWhenOneThrows()
    {
        // Arrange
        var publisher = new FakePublisher();
        var throwing = new ThrowingUserCreatedHandler();
        var alsoRuns = new RecordingUserCreatedHandler();
        publisher.AddHandler<UserCreated>(throwing);
        publisher.AddHandler<UserCreated>(alsoRuns);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            publisher.PublishAsync(new UserCreated(1), PublishStrategy.Parallel, TestContext.Current.CancellationToken)
        );
        Assert.Single(alsoRuns.Handled);
    }

    [Fact]
    public void AddHandler_ReturnsTheSameFakePublisherForChaining()
    {
        // Arrange
        var publisher = new FakePublisher();

        // Act
        var chained = publisher.AddHandler<UserCreated>(new RecordingUserCreatedHandler());

        // Assert
        Assert.Same(publisher, chained);
    }

    private sealed record UserCreated(int UserId) : INotification;

    private sealed record OrderShipped(int OrderId) : INotification;

    private sealed class RecordingUserCreatedHandler : INotificationHandler<UserCreated>
    {
        private readonly List<UserCreated> handled = [];

        public IReadOnlyList<UserCreated> Handled => this.handled;

        public Task HandleAsync(UserCreated notification, CancellationToken ct)
        {
            this.handled.Add(notification);
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingOrderShippedHandler : INotificationHandler<OrderShipped>
    {
        private readonly List<OrderShipped> handled = [];

        public IReadOnlyList<OrderShipped> Handled => this.handled;

        public Task HandleAsync(OrderShipped notification, CancellationToken ct)
        {
            this.handled.Add(notification);
            return Task.CompletedTask;
        }
    }

    private sealed class ThrowingUserCreatedHandler : INotificationHandler<UserCreated>
    {
        // Deliberately async rather than a synchronous throw: a real handler's exception surfaces through
        // its returned Task, not as a synchronous throw during Task.WhenAll's own enumeration of handlers -
        // this is what the parallel-strategy test below depends on to prove every handler still runs.
        public async Task HandleAsync(UserCreated notification, CancellationToken ct)
        {
            await Task.Yield();
            throw new InvalidOperationException("Simulated handler failure.");
        }
    }
}
