namespace Interlocutor.DependencyInjection.Abstractions;

using System.Diagnostics.CodeAnalysis;
using Interlocutor.Abstractions;
using Interlocutor.Abstractions.Commands;
using Interlocutor.Abstractions.Core;
using Interlocutor.Abstractions.Notifications;
using Interlocutor.Abstractions.Queries;
using Interlocutor.CommonKernel.ResultTypes;

/// <summary>
/// Decorates the library's real <see cref="Mediator"/> with the root-scope guard from ADR-0011: every
/// dispatch method (<c>Send</c>, <c>Publish</c>) checks <see cref="IRootScopeDetector.IsRootScope"/> first,
/// throwing <see cref="MediatorResolvedFromRootScopeException"/> - naming the request/notification type being
/// dispatched - instead of silently running the request against services resolved from the root
/// provider/container.
/// </summary>
/// <remarks>
/// <para>
/// Both container packages (<c>Interlocutor.DependencyInjection.Microsoft</c>,
/// <c>Interlocutor.DependencyInjection.Autofac</c>) register this type as the implementation behind
/// <see cref="IMediator"/>/<see cref="ISender"/>/<see cref="IPublisher"/>, wrapping a separately-registered
/// <see cref="Mediator"/> instance and their own <see cref="IRootScopeDetector"/> implementation. This keeps
/// <see cref="Mediator"/> itself free of any DI-specific root-detection logic - the guard is purely a
/// registration-time concern, applied here rather than inside dispatch itself - and keeps the guard
/// container-neutral, since it depends only on the <see cref="IRootScopeDetector"/> abstraction, not on
/// either concrete container.
/// </para>
/// <para>
/// The check is synchronous and runs before any work is done: a guarded dispatch that fails never resolves a
/// handler, never enters a pipeline behavior, and never touches the request beyond reading its runtime type
/// for the exception message.
/// </para>
/// </remarks>
public sealed class RootScopeGuardedMediator(IMediator inner, IRootScopeDetector rootScopeDetector) : IMediator
{
    /// <inheritdoc/>
    public Task<TResponse> Send<TResponse>(IQuery<TResponse> query, CancellationToken ct = default)
        where TResponse : IOutcome
    {
        ArgumentNullException.ThrowIfNull(query);
        this.EnsureNotRootScope(query.GetType());
        return inner.Send(query, ct);
    }

    /// <inheritdoc/>
    public Task<TResponse> Send<TResponse>(ICommand<TResponse> command, CancellationToken ct = default)
        where TResponse : IOutcome
    {
        ArgumentNullException.ThrowIfNull(command);
        this.EnsureNotRootScope(command.GetType());
        return inner.Send(command, ct);
    }

    /// <inheritdoc/>
    public Task<TResult> Send<TResult>(ICommand command, CancellationToken ct = default)
        where TResult : IOutcome
    {
        ArgumentNullException.ThrowIfNull(command);
        this.EnsureNotRootScope(command.GetType());
        return inner.Send<TResult>(command, ct);
    }

    /// <inheritdoc/>
    [RequiresUnreferencedCode(
        "May run the runtime-typed dispatch fallback when no generated dispatcher is registered - see "
            + "documentation/trim-and-aot-support.md."
    )]
    [RequiresDynamicCode(
        "May run the runtime-typed dispatch fallback when no generated dispatcher is registered - see "
            + "documentation/trim-and-aot-support.md."
    )]
    public Task<IOutcome> Send(object request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        this.EnsureNotRootScope(request.GetType());
        return inner.Send(request, ct);
    }

    /// <inheritdoc/>
    public Task Publish<TNotification>(TNotification notification, CancellationToken ct = default)
        where TNotification : INotification
    {
        ArgumentNullException.ThrowIfNull(notification);
        this.EnsureNotRootScope(notification.GetType());
        return inner.Publish(notification, ct);
    }

    /// <inheritdoc/>
    public Task Publish<TNotification>(TNotification notification, PublishStrategy strategy, CancellationToken ct = default)
        where TNotification : INotification
    {
        ArgumentNullException.ThrowIfNull(notification);
        this.EnsureNotRootScope(notification.GetType());
        return inner.Publish(notification, strategy, ct);
    }

    /// <summary>
    /// Throws <see cref="MediatorResolvedFromRootScopeException"/>, naming <paramref name="dispatchedType"/>,
    /// if <see cref="IRootScopeDetector.IsRootScope"/> reports this mediator was resolved from the root
    /// provider/container.
    /// </summary>
    /// <param name="dispatchedType">The runtime type of the request or notification being dispatched.</param>
    private void EnsureNotRootScope(Type dispatchedType)
    {
        if (rootScopeDetector.IsRootScope)
        {
            throw new MediatorResolvedFromRootScopeException(dispatchedType);
        }
    }
}
