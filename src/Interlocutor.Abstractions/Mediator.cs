namespace Interlocutor.Abstractions;

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Interlocutor.Abstractions.Commands;
using Interlocutor.Abstractions.Core;
using Interlocutor.Abstractions.Diagnostics;
using Interlocutor.Abstractions.Notifications;
using Interlocutor.Abstractions.Pipeline.Classifiers;
using Interlocutor.Abstractions.Queries;
using Interlocutor.CommonKernel.ResultTypes;

/// <summary>Default mediator that dispatches queries, commands, and notifications to their registered handlers
/// through pipeline behaviors. Handler wrapper instances are cached by request type to avoid repeated reflection.</summary>
/// <remarks>
/// <para>
/// Mediator acts as the central hub for CQRS request/response and publish/subscribe workflows. It resolves handlers
/// and pipeline behaviors from DI, composes them into a behavior chain, and executes the chain with the request.
/// </para>
/// <para>
/// Handler resolution is cached by request type to avoid expensive reflection on every dispatch. Five static caches
/// hold wrapper instances: one for queries, one for value-returning commands, one for void commands (keyed by both
/// command and result type), and two for notifications (one for sequential, one for parallel strategies).
/// </para>
/// <para>
/// Use <see cref="ISender"/> to dispatch queries/commands with typed responses, or <see cref="IPublisher"/> to
/// fire-and-forget notifications. <see cref="IMediator"/> combines both interfaces.
/// </para>
/// <para>
/// Every dispatch (<c>SendAsync</c> and <c>PublishAsync</c>, including the runtime-typed
/// <see cref="SendAsync(object, CancellationToken)"/> overload) starts an <see cref="Activity"/> via
/// <see cref="MediatorActivitySource.Source"/>, tagged with the
/// request type, the resolved handler type(s), and - for <c>SendAsync</c> - the resulting outcome case. This is built
/// into the mediator itself (see <see cref="MediatorActivitySource"/>), independent of and non-conflicting with the
/// opt-in <see cref="Pipeline.LoggingBehavior{TRequest,TResponse}"/>. When nothing listens to
/// <see cref="MediatorActivitySource.Name"/>, <see cref="ActivitySource.StartActivity(string, ActivityKind)"/>
/// returns <see langword="null"/> and every dispatch method takes a fast path that skips tracing entirely - no
/// allocation, no string formatting.
/// </para>
/// </remarks>
public sealed class Mediator(IServiceProvider services) : IMediator
{
    // Handler wrappers are cached by request type to avoid Activator.CreateInstance on every dispatch.
    // Each dictionary covers a distinct handler category: queries, value-returning commands,
    // void commands, and notifications (sequential and parallel strategies).
    private static readonly ConcurrentDictionary<Type, object> QueryWrappers = new();
    private static readonly ConcurrentDictionary<Type, object> CommandWrappers = new();
    private static readonly ConcurrentDictionary<(Type CommandType, Type ResultType), object> VoidCommandWrappers = new();
    private static readonly ConcurrentDictionary<Type, NotificationHandlerWrapper> SequentialNotificationWrappers = new();
    private static readonly ConcurrentDictionary<Type, NotificationHandlerWrapper> ParallelNotificationWrappers = new();

    /// <inheritdoc/>
    public Task<TResponse> SendAsync<TResponse>(IQuery<TResponse> query, CancellationToken ct = default)
        where TResponse : IOutcome
    {
        return DispatchWithTraceAsync<Type, IHandlerWrapper<TResponse>, TResponse>(
            QueryWrappers,
            query.GetType(),
            requestType =>
                Activator.CreateInstance(typeof(QueryHandlerWrapper<,>).MakeGenericType(requestType, typeof(TResponse)))!,
            query,
            (wrapper, act) => wrapper.HandleAsync(query, services, ct, act)
        );
    }

    /// <inheritdoc/>
    public Task<TResponse> SendAsync<TResponse>(ICommand<TResponse> command, CancellationToken ct = default)
        where TResponse : IOutcome
    {
        return DispatchWithTraceAsync<Type, IHandlerWrapper<TResponse>, TResponse>(
            CommandWrappers,
            command.GetType(),
            requestType =>
                Activator.CreateInstance(typeof(CommandHandlerWrapper<,>).MakeGenericType(requestType, typeof(TResponse)))!,
            command,
            (wrapper, act) => wrapper.HandleAsync(command, services, ct, act)
        );
    }

    /// <inheritdoc/>
    public Task<TResult> SendAsync<TResult>(ICommand command, CancellationToken ct = default)
        where TResult : IOutcome
    {
        // For void commands, cache by both command type and result type because the same command can be
        // handled with different outcome shapes depending on what the caller needs (e.g., caller may want
        // just Success/Failure, or may need additional cases like NotFound).
        return DispatchWithTraceAsync<(Type CommandType, Type ResultType), IVoidHandlerWrapper<TResult>, TResult>(
            VoidCommandWrappers,
            (command.GetType(), typeof(TResult)),
            key =>
                Activator.CreateInstance(
                    typeof(VoidCommandHandlerWrapper<,>).MakeGenericType(key.CommandType, key.ResultType)
                )!,
            command,
            (wrapper, act) => wrapper.HandleAsync(command, services, ct, act)
        );
    }

    /// <inheritdoc/>
    [RequiresUnreferencedCode(
        "SendAsync(object, CancellationToken) may run the runtime-typed dispatch fallback (see Mediator.SendAsync(object,...)'s remarks) - see documentation/trim-and-aot-support.md."
    )]
    [RequiresDynamicCode(
        "SendAsync(object, CancellationToken) may run the runtime-typed dispatch fallback - see documentation/trim-and-aot-support.md."
    )]
    public Task<IOutcome> SendAsync(object request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Routing (below, in DispatchAsync) either invokes the generated dispatcher's switch case or, on the
        // reflection fallback, the resolved concrete generic overload - either way a nested activity starts,
        // parenting naturally via Activity.Current the same way any handler-initiated nested Send call would.
        // This method's own activity represents the runtime-typed routing step itself.
        var activity = MediatorActivitySource.Source.StartActivity();
        return activity is null
            ? this.DispatchAsync(request, ct)
            : TraceAsync(activity, request, _ => this.DispatchAsync(request, ct));
    }

    /// <summary>
    /// Shared fast-path/traced-path scaffolding behind the three type-safe <c>SendAsync&lt;T&gt;</c> overloads
    /// (<see cref="IQuery{TResponse}"/>, <see cref="ICommand{TResponse}"/>, and <see cref="ICommand"/>): starts the
    /// dispatch <see cref="Activity"/> and either invokes <paramref name="dispatch"/> directly when nothing is
    /// listening, or routes it through <see cref="TraceAsync{TResult}"/> so the resulting outcome case (or a thrown
    /// exception) gets tagged on the activity.
    /// </summary>
    /// <typeparam name="TResponse">The outcome type returned by <paramref name="dispatch"/>.</typeparam>
    /// <param name="request">The original query/command, tagged on the activity for the traced path.</param>
    /// <param name="dispatch">
    /// Invokes the resolved handler wrapper's <c>HandleAsync</c> method, passing through whatever <see cref="Activity"/>
    /// it is given - <see langword="null"/> on the fast path, the started activity on the traced path - which
    /// matches <c>HandleAsync</c>'s own optional, defaulted-to-<see langword="null"/> activity parameter.
    /// </param>
    /// <returns>The task returned by <paramref name="dispatch"/>, awaited and tagged when traced.</returns>
    /// <remarks>
    /// Only the fast/traced branch is generic over the response type - wrapper resolution (cache lookup and
    /// construction) is handled by the <see cref="DispatchWithTraceAsync{TKey, TWrapper, TResponse}"/> overload, which
    /// closes over this one.
    /// </remarks>
    private static Task<TResponse> DispatchWithTraceAsync<TResponse>(object request, Func<Activity?, Task<TResponse>> dispatch)
    {
        // StartActivity returns null when nothing listens to MediatorActivitySource.Name - the fast path below
        // takes over immediately with no further tracing work, so the dispatch stays allocation- and
        // formatting-free when unobserved.
        var activity = MediatorActivitySource.Source.StartActivity();
        return activity is null ? dispatch(null) : TraceAsync(activity, request, act => dispatch(act));
    }

    /// <summary>
    /// Resolves (or lazily creates) the cached handler wrapper for <paramref name="key"/>, then dispatches through
    /// the fast-path/traced-path scaffolding in <see cref="DispatchWithTraceAsync{TResponse}"/>. Shared by all three
    /// type-safe <c>SendAsync&lt;T&gt;</c> overloads, each supplying only its own cache, key shape, and wrapper type -
    /// the cache lookup, <see cref="Activator.CreateInstance(Type)"/> construction, and activity-tracing branching
    /// live here once instead of being repeated per overload.
    /// </summary>
    /// <typeparam name="TKey">The cache's key type - a bare <see cref="Type"/> for query/command dispatch, or a
    /// <c>(Type CommandType, Type ResultType)</c> tuple for void-command dispatch, since the same void command can
    /// be handled with different outcome shapes depending on what the caller needs.</typeparam>
    /// <typeparam name="TWrapper">The resolved handler wrapper's interface - <see cref="IHandlerWrapper{TResponse}"/>
    /// or <see cref="IVoidHandlerWrapper{TResult}"/>.</typeparam>
    /// <typeparam name="TResponse">The outcome type <paramref name="invoke"/> ultimately returns.</typeparam>
    /// <param name="cache">The overload's own static wrapper cache.</param>
    /// <param name="key">The cache key identifying which wrapper to resolve or construct.</param>
    /// <param name="wrapperFactory">
    /// Constructs a new wrapper instance via reflection from <paramref name="key"/> when it is not yet cached.
    /// </param>
    /// <param name="request">The original query/command, tagged on the activity for the traced path.</param>
    /// <param name="invoke">Invokes the resolved wrapper's <c>HandleAsync</c> method with the given activity.</param>
    /// <returns>The task returned by <paramref name="invoke"/>, awaited and tagged when traced.</returns>
    private static Task<TResponse> DispatchWithTraceAsync<TKey, TWrapper, TResponse>(
        ConcurrentDictionary<TKey, object> cache,
        TKey key,
        Func<TKey, object> wrapperFactory,
        object request,
        Func<TWrapper, Activity?, Task<TResponse>> invoke
    )
        where TKey : notnull
    {
        var wrapper = (TWrapper)cache.GetOrAdd(key, wrapperFactory);
        return DispatchWithTraceAsync(request, act => invoke(wrapper, act));
    }

    /// <summary>
    /// Wraps a dispatch with activity tracing: tags the request type up front, invokes <paramref name="dispatch"/>
    /// (a closure over the resolved handler wrapper/routing call that tags the resolved handler type once it
    /// resolves it), tags the resulting outcome case on success when <typeparamref name="TResult"/> is an
    /// <see cref="IOutcome"/>, and records a thrown exception as an <see cref="ActivityStatusCode.Error"/>
    /// status rather than an outcome - before, either way, stopping the activity.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Only called once <see cref="MediatorActivitySource.Source"/> has already produced a non-null
    /// <paramref name="activity"/> - callers take a synchronous fast path when nothing is listening, so this method
    /// (and the async state machine and closure allocation it requires) is never reached on the unlistened dispatch
    /// path.
    /// </para>
    /// <para>
    /// Serves all three dispatch shapes the mediator traces - <c>SendAsync&lt;TResponse&gt;</c>'s and the runtime-typed
    /// <see cref="SendAsync(object, CancellationToken)"/>'s <see cref="IOutcome"/> result, and <c>PublishAsync</c>'s
    /// no-result <see cref="Task"/> - by checking <paramref name="dispatch"/>'s result for
    /// <see cref="IOutcome"/> at run time rather than constraining <typeparamref name="TResult"/> to it: a
    /// notification publish has no single outcome to tag (it may fan out to several handlers), so that call site
    /// passes a <see langword="null"/>-shaped <typeparamref name="TResult"/> and this method simply skips the
    /// outcome tag for it.
    /// </para>
    /// </remarks>
    private static async Task<TResult> TraceAsync<TResult>(
        Activity activity,
        object request,
        Func<Activity, Task<TResult>> dispatch
    )
    {
        // IDisposableAnalyzers treats any disposable parameter as "injected" and flags disposing it (IDISP007).
        // That heuristic doesn't see the actual ownership here: every caller creates `activity` via
        // MediatorActivitySource.Source.StartActivity() immediately before the call and hands it off solely to
        // dispatch it - this method is the sole, intended owner of its disposal.
#pragma warning disable IDISP007
        using (activity)
#pragma warning restore IDISP007
        {
            activity.SetTag(MediatorActivitySource.RequestTypeTag, request.GetType().FullName);
            try
            {
                var result = await dispatch(activity).ConfigureAwait(false);

                // An Outcome failure case is an expected business outcome, not a bug - record it as the
                // outcome it is, via the same classifier LoggingBehavior uses, rather than as an activity error.
                if (result is IOutcome outcome)
                {
                    activity.SetTag(
                        MediatorActivitySource.OutcomeTag,
                        OutcomeClassifierBuilder.DefaultClassifier.GetLabel(outcome)
                    );
                }

                return result;
            }
            catch (Exception ex)
            {
                // A thrown exception is a bug or infrastructure fault, not an expected outcome - record it as an
                // activity error rather than tagging a synthetic outcome case.
                activity.SetStatus(ActivityStatusCode.Error, ex.Message);
                throw;
            }
        }
    }

    /// <summary>
    /// Routes <paramref name="request"/> to the compile-time generated type switch when one is registered
    /// (<see cref="IGeneratedRequestDispatcher"/> - ticket 13), or to the reflection-based fallback
    /// (<see cref="DispatchAsync"/> - ticket 09) otherwise. Factored out of the public
    /// <see cref="SendAsync(object, CancellationToken)"/> overload so that method can take a synchronous,
    /// allocation-free fast path when activity tracing is unobserved, without this routing logic (and the
    /// async state machine it requires) running on that path.
    /// </summary>
    /// <remarks>
    /// <see cref="IGeneratedRequestDispatcher"/> is resolved fresh on every call rather than cached on this
    /// instance: <see cref="Mediator"/> itself is typically scoped (see the type's own remarks), and the
    /// generated dispatcher, when present, is a stateless singleton - a single <see cref="IServiceProvider.GetService(Type)"/>
    /// lookup per call is cheap relative to the reflection it replaces, and keeps this method free of extra
    /// constructor-time state.
    /// </remarks>
    [RequiresUnreferencedCode(
        "The runtime-typed dispatch fallback reflects over runtime types and is only reached when no IGeneratedRequestDispatcher "
            + "is registered (Interlocutor.Generator did not run, or its output was never passed to "
            + "AddGeneratedRegistrations) - see documentation/trim-and-aot-support.md."
    )]
    [RequiresDynamicCode(
        "The runtime-typed dispatch fallback closes generic types/methods reflectively - see documentation/trim-and-aot-support.md."
    )]
    private Task<IOutcome> DispatchAsync(object request, CancellationToken ct)
    {
        var generatedDispatcher = (IGeneratedRequestDispatcher?)services.GetService(typeof(IGeneratedRequestDispatcher));
        return generatedDispatcher is not null
            ? generatedDispatcher.DispatchAsync(request, services, ct)
            : ReflectionRequestDispatcher.DispatchAsync(this, services, request, ct);
    }

    /// <inheritdoc/>
    /// <typeparam name="TNotification">The notification type. Must implement <see cref="INotification"/>.</typeparam>
    /// <param name="notification">The notification to publish.</param>
    /// <param name="ct">A token that may be used to cancel the notification dispatch.</param>
    /// <returns>A task that completes when all registered handlers have finished processing the notification.</returns>
    /// <remarks><para>This overload uses the <see cref="PublishStrategy.Sequential"/> strategy by default.</para></remarks>
    public Task PublishAsync<TNotification>(TNotification notification, CancellationToken ct = default)
        where TNotification : INotification
    {
        return this.PublishHandlersAsync(notification, PublishStrategy.Sequential, ct);
    }

    /// <inheritdoc/>
    public Task PublishAsync<TNotification>(
        TNotification notification,
        PublishStrategy strategy,
        CancellationToken ct = default
    )
        where TNotification : INotification
    {
        return this.PublishHandlersAsync(notification, strategy, ct);
    }

    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2026:RequiresUnreferencedCode",
        Justification = "SequentialNotificationHandlerWrapper<>/ParallelNotificationHandlerWrapper<> are closed "
            + "only over the reference-typed notification's own runtime type (already DynamicallyAccessedMembers-"
            + "annotated as Interfaces on both wrapper classes) - see documentation/trim-and-aot-support.md."
    )]
    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2071:DynamicallyAccessedMembers",
        Justification = "notification.GetType() is the reference-typed notification's own runtime type, which "
            + "always satisfies DynamicallyAccessedMemberTypes.Interfaces in practice - the type is either the "
            + "caller's own compile-time-known TNotification (interfaces already preserved by ordinary usage) "
            + "or, for a polymorphic call, a concrete subtype that is likewise never trimmed away while it "
            + "remains constructible by the caller. See documentation/trim-and-aot-support.md."
    )]
    private Task PublishHandlersAsync<TNotification>(TNotification notification, PublishStrategy strategy, CancellationToken ct)
        where TNotification : INotification
    {
        // Select the wrapper cache and its matching wrapper type together, in one switch, based on the publish
        // strategy. Each strategy (sequential vs parallel) has its own cache to avoid mixing handler execution
        // modes. Falls back to sequential for unrecognized strategies.
        var (cache, wrapperType) = strategy switch
        {
            PublishStrategy.Parallel => (ParallelNotificationWrappers, typeof(ParallelNotificationHandlerWrapper<>)),
            _ => (SequentialNotificationWrappers, typeof(SequentialNotificationHandlerWrapper<>)),
        };

        // Get or create a notification wrapper (sequential or parallel) for this notification type.
        // The wrapper resolves all handlers from DI and orchestrates their execution according to the strategy.
        var wrapper = cache.GetOrAdd(
            notification.GetType(),
            notificationType =>
                (NotificationHandlerWrapper)Activator.CreateInstance(wrapperType.MakeGenericType(notificationType))!
        );

        var activity = MediatorActivitySource.Source.StartActivity();

        // Publish has no IOutcome to tag - notifications return Task, not an outcome, and may fan out
        // to several handlers - so TraceAsync's generic TResult is a placeholder, always-null nullable object.
        // TraceAsync's own outcome-tagging check is simply a no-op for it. See TraceAsync's remarks.
        return activity is null
            ? wrapper.HandleAsync(notification, services, ct)
            : TraceAsync(
                activity,
                notification,
                async act =>
                {
                    await wrapper.HandleAsync(notification, services, ct, act).ConfigureAwait(false);
                    return (object?)null;
                }
            );
    }
}
