namespace Interlocutor.Testing;

using Interlocutor.Abstractions.Core;
using Interlocutor.Abstractions.Notifications;

/// <summary>
/// A fake <see cref="IPublisher"/> for unit tests: records every notification passed to
/// <c>PublishAsync</c>, and optionally invokes fake handlers a test supplies, without wiring a real pipeline,
/// container, or handler registration.
/// </summary>
/// <remarks>
/// <para>
/// Construct one and pass it anywhere an <see cref="IPublisher"/> is expected. After the call under test
/// runs, assert against <see cref="PublishedNotifications"/> for what was published. If the test also needs
/// to assert on handler-side effects, supply the handler itself (an <see cref="INotificationHandler{TNotification}"/>
/// implementation the test constructs, or a recording test double) via
/// <see cref="AddHandler{TNotification}(INotificationHandler{TNotification})"/> before publishing - the fake
/// invokes it and the test asserts on what that handler observed, the same as it would with a real one.
/// </para>
/// <para>
/// Handler execution mirrors <see cref="IPublisher"/>'s own contract: <see cref="PublishStrategy.Sequential"/>
/// (the default) awaits handlers in registration order and stops at the first one that throws;
/// <see cref="PublishStrategy.Parallel"/> runs every registered handler via <see cref="Task.WhenAll(IEnumerable{Task})"/>
/// regardless of whether one throws. A notification type with no registered handler is still recorded - a
/// test asserting only on <see cref="PublishedNotifications"/> never needs to register one.
/// </para>
/// <para>
/// Not thread-safe: like most test doubles, a <see cref="FakePublisher"/> is expected to be exercised by one
/// test at a time, not shared across concurrently running tests.
/// </para>
/// </remarks>
public sealed class FakePublisher : IPublisher
{
    private readonly Dictionary<Type, List<object>> handlersByNotificationType = [];
    private readonly List<INotification> publishedNotifications = [];

    /// <summary>Gets every notification instance passed to either <c>PublishAsync</c> overload, in publish order.</summary>
    /// <value>A snapshot-free view backed by the list this fake appends to as notifications are published.</value>
    public IReadOnlyList<INotification> PublishedNotifications => this.publishedNotifications;

    /// <summary>
    /// Registers a fake handler this <see cref="FakePublisher"/> invokes whenever a
    /// <typeparamref name="TNotification"/> is published.
    /// </summary>
    /// <typeparam name="TNotification">The notification type <paramref name="handler"/> handles.</typeparam>
    /// <param name="handler">
    /// The handler to invoke - typically a recording test double the test later asserts against, but any
    /// <see cref="INotificationHandler{TNotification}"/> implementation works.
    /// </param>
    /// <returns>This <see cref="FakePublisher"/>, so calls can be chained when registering more than one handler.</returns>
    /// <remarks>
    /// Multiple handlers may be registered for the same <typeparamref name="TNotification"/>, mirroring
    /// <see cref="IPublisher"/>'s real many-handlers-per-notification contract - each runs in registration order.
    /// </remarks>
    public FakePublisher AddHandler<TNotification>(INotificationHandler<TNotification> handler)
        where TNotification : INotification
    {
        ArgumentNullException.ThrowIfNull(handler);

        if (!this.handlersByNotificationType.TryGetValue(typeof(TNotification), out var handlers))
        {
            handlers = [];
            this.handlersByNotificationType[typeof(TNotification)] = handlers;
        }

        handlers.Add(handler);
        return this;
    }

    /// <inheritdoc/>
    public Task PublishAsync<TNotification>(TNotification notification, CancellationToken ct = default)
        where TNotification : INotification => this.PublishAsync(notification, PublishStrategy.Sequential, ct);

    /// <inheritdoc/>
    public Task PublishAsync<TNotification>(
        TNotification notification,
        PublishStrategy strategy,
        CancellationToken ct = default
    )
        where TNotification : INotification
    {
        ArgumentNullException.ThrowIfNull(notification);
        this.publishedNotifications.Add(notification);
        return this.InvokeHandlersAsync(notification, strategy, ct);
    }

    /// <summary>Invokes every handler registered for <typeparamref name="TNotification"/>, per <paramref name="strategy"/>.</summary>
    /// <typeparam name="TNotification">The published notification's type.</typeparam>
    /// <param name="notification">The notification to hand to each registered handler.</param>
    /// <param name="strategy">Whether to await handlers in order (stopping on the first failure) or concurrently.</param>
    /// <param name="ct">The cancellation token passed through to every handler.</param>
    private async Task InvokeHandlersAsync<TNotification>(
        TNotification notification,
        PublishStrategy strategy,
        CancellationToken ct
    )
        where TNotification : INotification
    {
        if (!this.handlersByNotificationType.TryGetValue(typeof(TNotification), out var handlers))
        {
            return;
        }

        var typedHandlers = handlers.Cast<INotificationHandler<TNotification>>();
        if (strategy == PublishStrategy.Parallel)
        {
            await Task.WhenAll(typedHandlers.Select(handler => handler.HandleAsync(notification, ct))).ConfigureAwait(false);
            return;
        }

        foreach (var handler in typedHandlers)
        {
            await handler.HandleAsync(notification, ct).ConfigureAwait(false);
        }
    }
}
