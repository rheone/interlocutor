using System.Collections.Concurrent;
using System.Diagnostics;
using Mediance.Abstractions.Commands;
using Mediance.Abstractions.Core;
using Mediance.Abstractions.Notifications;
using Mediance.Abstractions.Pipeline;
using Mediance.Abstractions.Queries;
using Mediance.CommonKernel;

namespace Mediance.Abstractions;

/// <summary>Defines the base contract for handlers that produce a typed response.</summary>
/// <typeparam name="TResponse">The type of the success value produced by the handler.</typeparam>
internal abstract class HandlerWrapper<TResponse>
{
    public abstract Task<Result<TResponse>> Handle(object request, IServiceProvider services, CancellationToken ct);
}

/// <summary>Defines the base contract for handlers that produce a void (unit) result.</summary>
internal abstract class VoidHandlerWrapper
{
    public abstract Task<Result> Handle(object request, IServiceProvider services, CancellationToken ct);
}

internal sealed class VoidCommandHandlerWrapper<TCommand> : VoidHandlerWrapper
    where TCommand : ICommand
{
    public override async Task<Result> Handle(object request, IServiceProvider services, CancellationToken ct)
    {
        var typedRequest = (TCommand)request;
        var handler = services.GetRequiredService<ICommandHandler<TCommand>>();

        // Wrap the void handler's Result into Result<Result> so the pipeline
        // can treat it uniformly with value-returning command pipelines.
        // The outer Result is unwrapped below after the pipeline completes.
        Func<Task<Result<Result>>> pipeline = async () =>
        {
            var r = await handler.Handle(typedRequest, ct);
            return r.Value switch
            {
                Success s => new Result<Result>(s),
                Failure f => new Result<Result>(f),
                _ => throw new UnreachableException(),
            };
        };

        foreach (var behavior in services.GetServices<IPipelineBehavior<TCommand, Result>>().Reverse())
        {
            var next = pipeline;
            pipeline = () => behavior.Handle(typedRequest, next, ct);
        }

        // Unwrap the pipeline's Result<Result> back to the original Result.
        var pipelineResult = await pipeline();
        return pipelineResult.Value switch
        {
            Result r => r,
            Failure f => new Result(f),
            _ => throw new UnreachableException(),
        };
    }
}

/// <summary>Wraps a query handler, constructing the pipeline chain around it at invocation time.</summary>
/// <typeparam name="TQuery">The query type.</typeparam>
/// <typeparam name="TResponse">The expected response type.</typeparam>
internal sealed class QueryHandlerWrapper<TQuery, TResponse> : HandlerWrapper<TResponse>
    where TQuery : IQuery<TResponse>
{
    public override async Task<Result<TResponse>> Handle(object request, IServiceProvider services, CancellationToken ct)
    {
        var typedRequest = (TQuery)request;
        var handler = services.GetRequiredService<IQueryHandler<TQuery, TResponse>>();

        Func<Task<Result<TResponse>>> pipeline = () => handler.Handle(typedRequest, ct);

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
    public override async Task<Result<TResponse>> Handle(object request, IServiceProvider services, CancellationToken ct)
    {
        var typedRequest = (TCommand)request;
        var handler = services.GetRequiredService<ICommandHandler<TCommand, TResponse>>();

        Func<Task<Result<TResponse>>> pipeline = () => handler.Handle(typedRequest, ct);

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
    private static readonly ConcurrentDictionary<Type, VoidHandlerWrapper> VoidCommandWrappers = new();
    private static readonly ConcurrentDictionary<Type, NotificationHandlerWrapper> SequentialNotificationWrappers = new();
    private static readonly ConcurrentDictionary<Type, NotificationHandlerWrapper> ParallelNotificationWrappers = new();

    /// <inheritdoc/>
    public Task<Result<TResponse>> Send<TResponse>(IQuery<TResponse> query, CancellationToken ct = default)
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
    public Task<Result<TResponse>> Send<TResponse>(ICommand<TResponse> command, CancellationToken ct = default)
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
    public Task<Result> Send(ICommand command, CancellationToken ct = default)
    {
        var wrapper = VoidCommandWrappers.GetOrAdd(
            command.GetType(),
            requestType =>
                (VoidHandlerWrapper)Activator.CreateInstance(typeof(VoidCommandHandlerWrapper<>).MakeGenericType(requestType))!
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
