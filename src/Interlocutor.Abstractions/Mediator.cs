namespace Interlocutor.Abstractions;

using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using Interlocutor.Abstractions.Commands;
using Interlocutor.Abstractions.Core;
using Interlocutor.Abstractions.Notifications;
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

    // A unit command (ICommand) does not declare its own response type — the caller picks it via Send<TResult>'s
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

        return wrapper.Handle(query, services, ct);
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

        return wrapper.Handle(command, services, ct);
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

        return wrapper.Handle(command, services, ct);
    }

    /// <inheritdoc/>
    public async Task<IOperationOutcome> Send(object request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

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
            // A unit command doesn't declare its own response type — Send<TResult>'s caller-supplied type
            // argument picks it. Runtime-typed dispatch has no caller-supplied type argument, so the response
            // type is discovered by scanning loaded assemblies for a registered IVoidCommandHandler<TCommand,
            // TResult> closing over this concrete command type (cached per command type — see the field doc).
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
    /// result as the base outcome interface — the reflection stand-in for the compile-time generic type
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
    /// field doc) — a future generator-backed implementation replaces it with a compiled lookup, without
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

        return wrapper.Handle(notification, services, ct);
    }
}
