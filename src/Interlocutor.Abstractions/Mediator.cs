namespace Interlocutor.Abstractions;

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Interlocutor.Abstractions.Commands;
using Interlocutor.Abstractions.Core;
using Interlocutor.Abstractions.Diagnostics;
using Interlocutor.Abstractions.Notifications;
using Interlocutor.Abstractions.Pipeline.Classifiers;
using Interlocutor.Abstractions.Queries;
using Interlocutor.CommonKernel;

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
/// Every dispatch (<c>Send</c> and <c>Publish</c>, including the runtime-typed <see cref="Send(object, CancellationToken)"/>
/// overload) starts an <see cref="Activity"/> via <see cref="MediatorActivitySource.Source"/>, tagged with the
/// request type, the resolved handler type(s), and - for <c>Send</c> - the resulting outcome case. This is built
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

    // Runtime-typed dispatch (Send(object, ...)) reuses the strongly-typed Send<TResponse> overloads above via
    // reflection, rather than duplicating pipeline-composition logic. These MethodInfo handles are resolved once
    // (each generic method definition is found by matching its first parameter's open generic shape) and reused
    // for every call, since MakeGenericMethod on an already-resolved MethodInfo is cheap relative to re-scanning
    // the type's methods.
    private static readonly MethodInfo QuerySendMethod = ResolveSendMethod(typeof(IQuery<>));
    private static readonly MethodInfo CommandSendMethod = ResolveSendMethod(typeof(ICommand<>));
    private static readonly MethodInfo VoidCommandSendMethod = ResolveSendMethod(typeof(ICommand));

    // A unit command (ICommand) does not declare its own response type - the caller picks it via Send<TResult>'s
    // type argument. Runtime-typed dispatch has no caller-supplied type argument, so it discovers the response
    // type by scanning loaded assemblies for a registered IVoidCommandHandler<TCommand, TResult> closing over the
    // concrete command type. This is the expensive part of the reflection fallback; results are cached per command
    // type since the answer cannot change without reloading assemblies.
    private static readonly ConcurrentDictionary<Type, Type?> VoidCommandResultTypes = new();

    /// <inheritdoc/>
    public Task<TResponse> Send<TResponse>(IQuery<TResponse> query, CancellationToken ct = default)
        where TResponse : IOperationOutcome
    {
        // Look up or create the handler wrapper for this query type. The wrapper composes the pipeline
        // behavior chain at invocation time. Caching by query type avoids expensive reflection for every dispatch.
        var wrapper =
            (HandlerWrapper<TResponse>)
                QueryWrappers.GetOrAdd(
                    query.GetType(),
                    requestType =>
                        Activator.CreateInstance(
                            typeof(QueryHandlerWrapper<,>).MakeGenericType(requestType, typeof(TResponse))
                        )!
                );

        // StartActivity returns null when nothing listens to MediatorActivitySource.Name - the fast path below
        // takes over immediately with no further tracing work, so the dispatch stays allocation- and
        // formatting-free when unobserved.
        var activity = MediatorActivitySource.Source.StartActivity();
        return activity is null
            ? wrapper.Handle(query, services, ct)
            : TraceSendAsync(activity, query, (req, act) => wrapper.Handle(req, services, ct, act));
    }

    /// <inheritdoc/>
    public Task<TResponse> Send<TResponse>(ICommand<TResponse> command, CancellationToken ct = default)
        where TResponse : IOperationOutcome
    {
        // Look up or create the handler wrapper for this command type. The wrapper composes the pipeline
        // behavior chain at invocation time. Caching by command type avoids expensive reflection for every dispatch.
        var wrapper =
            (HandlerWrapper<TResponse>)
                CommandWrappers.GetOrAdd(
                    command.GetType(),
                    requestType =>
                        Activator.CreateInstance(
                            typeof(CommandHandlerWrapper<,>).MakeGenericType(requestType, typeof(TResponse))
                        )!
                );

        var activity = MediatorActivitySource.Source.StartActivity();
        return activity is null
            ? wrapper.Handle(command, services, ct)
            : TraceSendAsync(activity, command, (req, act) => wrapper.Handle(req, services, ct, act));
    }

    /// <inheritdoc/>
    public Task<TResult> Send<TResult>(ICommand command, CancellationToken ct = default)
        where TResult : IOperationOutcome
    {
        // For void commands, cache by both command type and result type because the same command can be
        // handled with different outcome shapes depending on what the caller needs (e.g., caller may want
        // just Success/Failure, or may need additional cases like NotFound).
        var wrapper =
            (VoidHandlerWrapper<TResult>)
                VoidCommandWrappers.GetOrAdd(
                    (command.GetType(), typeof(TResult)),
                    key =>
                        Activator.CreateInstance(
                            typeof(VoidCommandHandlerWrapper<,>).MakeGenericType(key.CommandType, key.ResultType)
                        )!
                );

        var activity = MediatorActivitySource.Source.StartActivity();
        return activity is null
            ? wrapper.Handle(command, services, ct)
            : TraceSendAsync(activity, command, (req, act) => wrapper.Handle(req, services, ct, act));
    }

    /// <summary>
    /// Wraps a strongly-typed <c>Send</c> dispatch with activity tracing: tags the request type up front, invokes
    /// <paramref name="dispatch"/> (a closure over the resolved <c>HandlerWrapper</c>/<c>VoidHandlerWrapper</c> that
    /// tags the resolved handler type once it resolves it), tags the resulting outcome case on success, and records
    /// a thrown exception as an <see cref="ActivityStatusCode.Error"/> status rather than an outcome - before,
    /// either way, stopping the activity.
    /// </summary>
    /// <remarks>
    /// Only called once <see cref="MediatorActivitySource.Source"/> has already produced a non-null
    /// <paramref name="activity"/> - callers take a synchronous fast path when nothing is listening, so this method
    /// (and the async state machine and closure allocation it requires) is never reached on the unlistened dispatch
    /// path. Taking a <see cref="Func{T1,T2,TResult}"/> rather than the wrapper type directly lets one method serve
    /// both <see cref="HandlerWrapper{TResponse}"/> and <see cref="VoidHandlerWrapper{TResult}"/> callers, which
    /// share no common base.
    /// </remarks>
    private static async Task<TResponse> TraceSendAsync<TResponse>(
        Activity activity,
        object request,
        Func<object, Activity, Task<TResponse>> dispatch
    )
        where TResponse : IOperationOutcome
    {
        using (activity)
        {
            activity.SetTag(MediatorActivitySource.RequestTypeTag, request.GetType().FullName);
            try
            {
                var result = await dispatch(request, activity).ConfigureAwait(false);

                // An OperationOutcome failure case is an expected business outcome, not a bug - record it as the
                // outcome it is, via the same classifier LoggingBehavior uses, rather than as an activity error.
                activity.SetTag(MediatorActivitySource.OutcomeTag, OutcomeClassifierBuilder.DefaultClassifier.GetLabel(result));
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

    /// <inheritdoc/>
    public Task<IOperationOutcome> Send(object request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Routing (below, in DispatchAsync) resolves the concrete generic overload and invokes it reflectively,
        // which starts its own nested activity - the two naturally parent via Activity.Current, the same way any
        // handler-initiated nested Send call would. This method's own activity represents the runtime-typed
        // routing step itself.
        var activity = MediatorActivitySource.Source.StartActivity();
        return activity is null ? this.DispatchAsync(request, ct) : TraceDispatchAsync(activity, request, this, ct);
    }

    /// <summary>
    /// Wraps <see cref="DispatchAsync"/> with the same activity-tracing shape as <see cref="TraceSendAsync{TResponse}"/>,
    /// but for the runtime-typed overload's <see cref="IOperationOutcome"/> return type.
    /// </summary>
    private static async Task<IOperationOutcome> TraceDispatchAsync(
        Activity activity,
        object request,
        Mediator instance,
        CancellationToken ct
    )
    {
        using (activity)
        {
            activity.SetTag(MediatorActivitySource.RequestTypeTag, request.GetType().FullName);
            try
            {
                var result = await instance.DispatchAsync(request, ct).ConfigureAwait(false);
                activity.SetTag(MediatorActivitySource.OutcomeTag, OutcomeClassifierBuilder.DefaultClassifier.GetLabel(result));
                return result;
            }
            catch (Exception ex)
            {
                activity.SetStatus(ActivityStatusCode.Error, ex.Message);
                throw;
            }
        }
    }

    /// <summary>
    /// Resolves and invokes the correct strongly-typed <c>Send</c> overload for <paramref name="request"/>'s
    /// runtime type, via reflection. Factored out of the public <see cref="Send(object, CancellationToken)"/>
    /// overload so that method can take a synchronous, allocation-free fast path when activity tracing is
    /// unobserved, without this routing logic (and the async state machine it requires) running on that path.
    /// </summary>
    private async Task<IOperationOutcome> DispatchAsync(object request, CancellationToken ct)
    {
        var requestType = request.GetType();

        var queryInterface = FindClosedInterface(requestType, typeof(IQuery<>));
        if (queryInterface is not null)
        {
            var responseType = queryInterface.GetGenericArguments()[0];
            EnsureHandlerRegistered(services, requestType, typeof(IQueryHandler<,>).MakeGenericType(requestType, responseType));
            return await InvokeSendAsync(QuerySendMethod, this, responseType, request, ct).ConfigureAwait(false);
        }

        var commandInterface = FindClosedInterface(requestType, typeof(ICommand<>));
        if (commandInterface is not null)
        {
            var responseType = commandInterface.GetGenericArguments()[0];
            EnsureHandlerRegistered(
                services,
                requestType,
                typeof(ICommandHandler<,>).MakeGenericType(requestType, responseType)
            );
            return await InvokeSendAsync(CommandSendMethod, this, responseType, request, ct).ConfigureAwait(false);
        }

        if (request is ICommand)
        {
            // A unit command doesn't declare its own response type - Send<TResult>'s caller-supplied type
            // argument picks it. Runtime-typed dispatch has no caller-supplied type argument, so the response
            // type is discovered by scanning loaded assemblies for a registered IVoidCommandHandler<TCommand,
            // TResult> closing over this concrete command type (cached per command type - see the field doc).
            var resultType = VoidCommandResultTypes.GetOrAdd(requestType, ResolveVoidCommandResultType);
            if (resultType is null)
            {
                throw new HandlerNotRegisteredException(requestType);
            }

            EnsureHandlerRegistered(
                services,
                requestType,
                typeof(IVoidCommandHandler<,>).MakeGenericType(requestType, resultType)
            );
            return await InvokeSendAsync(VoidCommandSendMethod, this, resultType, request, ct).ConfigureAwait(false);
        }

        throw new UnsupportedRequestTypeException(requestType);
    }

    /// <summary>
    /// Resolves the <see cref="MethodInfo"/> for one of <see cref="ISender"/>'s generic <c>Send</c> overloads by
    /// matching its first parameter's shape, so runtime-typed dispatch can invoke it reflectively without
    /// duplicating the pipeline-composition logic each overload already carries.
    /// </summary>
    /// <param name="requestParameterType">
    /// The open generic interface (e.g. <c>typeof(IQuery&lt;&gt;)</c>) or, for the unit-command overload, the
    /// non-generic <see cref="ICommand"/> type, identifying which overload's first parameter to match.
    /// </param>
    private static MethodInfo ResolveSendMethod(Type requestParameterType) =>
        typeof(ISender)
            .GetMethods()
            .Single(method =>
            {
                var parameters = method.GetParameters();
                if (parameters.Length == 0)
                {
                    return false;
                }

                var parameterType = parameters[0].ParameterType;
                return requestParameterType.IsGenericTypeDefinition
                    ? parameterType.IsGenericType && parameterType.GetGenericTypeDefinition() == requestParameterType
                    : parameterType == requestParameterType;
            });

    /// <summary>
    /// Invokes a resolved generic <c>Send&lt;TResponse&gt;</c> overload reflectively, awaits it, and returns its
    /// result as the base outcome interface - the reflection stand-in for the compile-time generic type
    /// argument a static caller would normally supply.
    /// </summary>
    private static async Task<IOperationOutcome> InvokeSendAsync(
        MethodInfo openGenericSendMethod,
        Mediator instance,
        Type responseType,
        object request,
        CancellationToken ct
    )
    {
        var closedMethod = openGenericSendMethod.MakeGenericMethod(responseType);
        var task = (Task)closedMethod.Invoke(instance, [request, ct])!;
        await task.ConfigureAwait(false);

        // Reflectively read Task<TResponse>.Result rather than casting to a statically-typed Task<TResponse> -
        // TResponse is only known at this point as a runtime Type, not a compile-time generic argument. Result
        // is safe to read unconditionally here: the preceding await already completed (or faulted/threw out of)
        // the task, so a completed Task<T> always has a non-null Result property to reflect.
        var resultProperty = task.GetType().GetProperty("Result")!;
        return (IOperationOutcome)resultProperty.GetValue(task)!;
    }

    /// <summary>
    /// Finds the closed generic interface matching <paramref name="openGenericInterface"/> that
    /// <paramref name="requestType"/> implements, or <see langword="null"/> if it implements none.
    /// </summary>
    private static Type? FindClosedInterface(Type requestType, Type openGenericInterface) =>
        requestType
            .GetInterfaces()
            .FirstOrDefault(candidate =>
                candidate.IsGenericType && candidate.GetGenericTypeDefinition() == openGenericInterface
            );

    /// <summary>
    /// Throws <see cref="HandlerNotRegisteredException"/> if <paramref name="handlerType"/> has no registration
    /// in <paramref name="services"/>. Runtime-typed dispatch checks this explicitly, rather than letting a
    /// missing handler surface as the DI container's own generic exception from deep inside a handler wrapper.
    /// </summary>
    private static void EnsureHandlerRegistered(IServiceProvider services, Type requestType, Type handlerType)
    {
        if (services.GetService(handlerType) is null)
        {
            throw new HandlerNotRegisteredException(requestType, handlerType);
        }
    }

    /// <summary>
    /// Scans loaded assemblies for a concrete type implementing <c>IVoidCommandHandler&lt;TCommand, TResult&gt;</c>
    /// closed over <paramref name="commandType"/>, returning the <c>TResult</c> it was implemented against, or
    /// <see langword="null"/> if no such implementation is loaded.
    /// </summary>
    /// <remarks>
    /// This is the expensive part of the reflection-based fallback (see the <see cref="VoidCommandResultTypes"/>
    /// field doc) - a future generator-backed implementation replaces it with a compiled lookup, without
    /// changing <see cref="Send(object, CancellationToken)"/>'s contract.
    /// </remarks>
    private static Type? ResolveVoidCommandResultType(Type commandType)
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type[] types;
            try
            {
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                types = ex.Types.Where(type => type is not null).Cast<Type>().ToArray();
            }

            foreach (var candidate in types)
            {
                if (candidate.IsAbstract || candidate.IsInterface)
                {
                    continue;
                }

                foreach (var candidateInterface in candidate.GetInterfaces())
                {
                    if (
                        !candidateInterface.IsGenericType
                        || candidateInterface.GetGenericTypeDefinition() != typeof(IVoidCommandHandler<,>)
                    )
                    {
                        continue;
                    }

                    var genericArguments = candidateInterface.GetGenericArguments();
                    if (genericArguments[0] == commandType)
                    {
                        return genericArguments[1];
                    }
                }
            }
        }

        return null;
    }

    /// <inheritdoc/>
    /// <typeparam name="TNotification">The notification type. Must implement <see cref="INotification"/>.</typeparam>
    /// <param name="notification">The notification to publish.</param>
    /// <param name="ct">A token that may be used to cancel the notification dispatch.</param>
    /// <returns>A task that completes when all registered handlers have finished processing the notification.</returns>
    /// <remarks><para>This overload uses the <see cref="PublishStrategy.Sequential"/> strategy by default.</para></remarks>
    public Task Publish<TNotification>(TNotification notification, CancellationToken ct = default)
        where TNotification : INotification
    {
        return this.PublishAsync(notification, PublishStrategy.Sequential, ct);
    }

    /// <summary>Publishes a notification using the specified <paramref name="strategy"/>.</summary>
    /// <typeparam name="TNotification">The notification type. Must implement <see cref="INotification"/>.</typeparam>
    /// <param name="notification">The notification to publish.</param>
    /// <param name="strategy"><see cref="PublishStrategy.Sequential"/> runs handlers in order and stops on failure; <see cref="PublishStrategy.Parallel"/> runs all handlers concurrently.</param>
    /// <param name="ct">A token that may be used to cancel the notification dispatch.</param>
    /// <returns>A task that completes when all registered handlers have finished processing the notification.</returns>
    public Task Publish<TNotification>(TNotification notification, PublishStrategy strategy, CancellationToken ct = default)
        where TNotification : INotification
    {
        return this.PublishAsync(notification, strategy, ct);
    }

    private Task PublishAsync<TNotification>(TNotification notification, PublishStrategy strategy, CancellationToken ct)
        where TNotification : INotification
    {
        // Select the appropriate wrapper cache based on the publish strategy. Each strategy (sequential vs parallel)
        // has its own cache to avoid mixing handler execution modes. Falls back to sequential for unrecognized strategies.
        var cache = strategy switch
        {
            PublishStrategy.Sequential => SequentialNotificationWrappers,
            PublishStrategy.Parallel => ParallelNotificationWrappers,
            _ => SequentialNotificationWrappers,
        };

        // Get or create a notification wrapper (sequential or parallel) for this notification type.
        // The wrapper resolves all handlers from DI and orchestrates their execution according to the strategy.
        var wrapper = cache.GetOrAdd(
            notification.GetType(),
            notificationType =>
            {
                var wrapperType = strategy switch
                {
                    PublishStrategy.Sequential => typeof(SequentialNotificationHandlerWrapper<>),
                    PublishStrategy.Parallel => typeof(ParallelNotificationHandlerWrapper<>),
                    _ => typeof(SequentialNotificationHandlerWrapper<>),
                };

                return (NotificationHandlerWrapper)Activator.CreateInstance(wrapperType.MakeGenericType(notificationType))!;
            }
        );

        var activity = MediatorActivitySource.Source.StartActivity();
        return activity is null
            ? wrapper.Handle(notification, services, ct)
            : TracePublishAsync(activity, notification, wrapper, services, ct);
    }

    /// <summary>
    /// Wraps a <c>Publish</c> dispatch with activity tracing: tags the notification type up front, tags the
    /// resolved handler type(s) once the wrapper resolves them (via the <paramref name="activity"/> passed through
    /// to <see cref="NotificationHandlerWrapper.Handle"/>), and records a thrown exception as an
    /// <see cref="ActivityStatusCode.Error"/> status - before, either way, stopping the activity.
    /// </summary>
    /// <remarks>
    /// Publish has no <see cref="IOperationOutcome"/> to tag: notifications return <see cref="Task"/>, not an
    /// outcome, and may fan out to several handlers, so unlike <see cref="TraceSendAsync{TResponse}"/> there is no
    /// single outcome case to record here.
    /// </remarks>
    private static async Task TracePublishAsync(
        Activity activity,
        object notification,
        NotificationHandlerWrapper wrapper,
        IServiceProvider services,
        CancellationToken ct
    )
    {
        using (activity)
        {
            activity.SetTag(MediatorActivitySource.RequestTypeTag, notification.GetType().FullName);
            try
            {
                await wrapper.Handle(notification, services, ct, activity).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                activity.SetStatus(ActivityStatusCode.Error, ex.Message);
                throw;
            }
        }
    }
}
