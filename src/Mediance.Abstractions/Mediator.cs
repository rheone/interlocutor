namespace Mediance.Abstractions;

using System.Collections.Concurrent;
using Mediance.Abstractions.Commands;
using Mediance.Abstractions.Core;
using Mediance.Abstractions.Notifications;
using Mediance.Abstractions.Pipeline;
using Mediance.Abstractions.Queries;
using Mediance.CommonKernel;

/// <summary>Defines the base contract for handlers that produce a typed response.</summary>
/// <typeparam name="TResponse">The type of the success value produced by the handler.</typeparam>
internal abstract class HandlerWrapper<TResponse>
{
    public abstract Task<TResponse> Handle(object request, IServiceProvider services, CancellationToken ct);
}

/// <summary>Defines the base contract for handlers that produce an outcome-only (no data payload) result.</summary>
/// <typeparam name="TResult">The <see cref="IResult"/>-shaped outcome type produced by the handler.</typeparam>
internal abstract class VoidHandlerWrapper<TResult>
    where TResult : IResult
{
    public abstract Task<TResult> Handle(object request, IServiceProvider services, CancellationToken ct);
}

internal sealed class VoidCommandHandlerWrapper<TCommand, TResult> : VoidHandlerWrapper<TResult>
    where TCommand : ICommand
    where TResult : IResult
{
    public override async Task<TResult> Handle(object request, IServiceProvider services, CancellationToken ct)
    {
        var typedRequest = (TCommand)request;
        var handler = services.GetRequiredService<IVoidCommandHandler<TCommand, TResult>>();

        Func<Task<TResult>> pipeline = () => handler.Handle(typedRequest, ct);

        foreach (var behavior in services.GetServices<IPipelineBehavior<TCommand, TResult>>().Reverse())
        {
            var next = pipeline;
            pipeline = () => behavior.Handle(typedRequest, next, ct);
        }

        return await pipeline();
    }
}

/// <summary>Wraps a query handler, constructing the pipeline chain around it at invocation time.</summary>
/// <typeparam name="TQuery">The query type.</typeparam>
/// <typeparam name="TResponse">The expected response type.</typeparam>
internal sealed class QueryHandlerWrapper<TQuery, TResponse> : HandlerWrapper<TResponse>
    where TQuery : IQuery<TResponse>
{
    public override async Task<TResponse> Handle(object request, IServiceProvider services, CancellationToken ct)
    {
        var typedRequest = (TQuery)request;
        var handler = services.GetRequiredService<IQueryHandler<TQuery, TResponse>>();

        var pipeline = () => handler.Handle(typedRequest, ct);

        foreach (var behavior in services.GetServices<IPipelineBehavior<TQuery, TResponse>>().Reverse())
        {
            var next = pipeline;
            pipeline = () => behavior.Handle(typedRequest, next, ct);
        }

        return await pipeline();
    }
}

/// <summary>Wraps a value-returning command handler, constructing the pipeline chain around it at invocation time.</summary>
/// <typeparam name="TCommand">The command type.</typeparam>
/// <typeparam name="TResponse">The expected response type.</typeparam>
internal sealed class CommandHandlerWrapper<TCommand, TResponse> : HandlerWrapper<TResponse>
    where TCommand : ICommand<TResponse>
{
    public override async Task<TResponse> Handle(object request, IServiceProvider services, CancellationToken ct)
    {
        var typedRequest = (TCommand)request;
        var handler = services.GetRequiredService<ICommandHandler<TCommand, TResponse>>();

        Func<Task<TResponse>> pipeline = () => handler.Handle(typedRequest, ct);

        foreach (var behavior in services.GetServices<IPipelineBehavior<TCommand, TResponse>>().Reverse())
        {
            var next = pipeline;
            pipeline = () => behavior.Handle(typedRequest, next, ct);
        }

        return await pipeline();
    }
}

/// <summary>Defines the base contract for notification dispatch wrappers.</summary>
internal abstract class NotificationHandlerWrapper
{
    public abstract Task Handle(object notification, IServiceProvider services, CancellationToken ct);
}

/// <summary>Dispatches a notification to all registered handlers sequentially, stopping on the first failure.</summary>
/// <typeparam name="TNotification">The notification type. Must implement <see cref="INotification"/>.</typeparam>
internal sealed class SequentialNotificationHandlerWrapper<TNotification> : NotificationHandlerWrapper
    where TNotification : INotification
{
    public override async Task Handle(object notification, IServiceProvider services, CancellationToken ct)
    {
        var typedNotification = (TNotification)notification;
        var handlers = services.GetServices<INotificationHandler<TNotification>>();

        foreach (var handler in handlers)
        {
            await handler.Handle(typedNotification, ct);
        }
    }
}

/// <summary>Dispatches a notification to all registered handlers concurrently using <see cref="Task.WhenAll(System.Collections.Generic.IEnumerable{System.Threading.Tasks.Task})"/>.</summary>
/// <typeparam name="TNotification">The notification type. Must implement <see cref="INotification"/>.</typeparam>
internal sealed class ParallelNotificationHandlerWrapper<TNotification> : NotificationHandlerWrapper
    where TNotification : INotification
{
    public override async Task Handle(object notification, IServiceProvider services, CancellationToken ct)
    {
        var typedNotification = (TNotification)notification;
        var handlers = services.GetServices<INotificationHandler<TNotification>>();
        var tasks = handlers.Select(h => h.Handle(typedNotification, ct));
        await Task.WhenAll(tasks);
    }
}

/// <summary>Default mediator that dispatches queries, commands, and notifications to their registered handlers
/// through pipeline behaviors. Handler wrapper instances are cached by request type to avoid repeated reflection.</summary>
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
    public Task<TResponse> Send<TResponse>(IQuery<TResponse> query, CancellationToken ct = default)
    {
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
    {
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
        where TResult : IResult
    {
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
    /// <typeparam name="TNotification">The notification type. Must implement <see cref="INotification"/>.</typeparam>
    /// <param name="notification">The notification to publish.</param>
    /// <param name="ct">A token that may be used to cancel the notification dispatch.</param>
    /// <returns>A task that completes when all registered handlers have finished processing the notification.</returns>
    /// <remarks><para>This overload uses the <see cref="PublishStrategy.Sequential"/> strategy by default.</para></remarks>
    public Task Publish<TNotification>(TNotification notification, CancellationToken ct = default)
        where TNotification : INotification
    {
        return PublishAsync(notification, PublishStrategy.Sequential, ct);
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
        return PublishAsync(notification, strategy, ct);
    }

    private Task PublishAsync<TNotification>(TNotification notification, PublishStrategy strategy, CancellationToken ct)
        where TNotification : INotification
    {
        // Select the wrapper cache based on strategy. Falls back to sequential
        // caching for unrecognized strategies rather than throwing.
        var cache = strategy switch
        {
            PublishStrategy.Sequential => SequentialNotificationWrappers,
            PublishStrategy.Parallel => ParallelNotificationWrappers,
            _ => SequentialNotificationWrappers,
        };

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
