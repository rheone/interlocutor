using System.Collections.Concurrent;
using System.Diagnostics;
using Mediance.Abstractions.Commands;
using Mediance.Abstractions.Core;
using Mediance.Abstractions.Notifications;
using Mediance.Abstractions.Pipeline;
using Mediance.Abstractions.Queries;
using Mediance.CommonKernel;

namespace Mediance.Abstractions;

internal abstract class HandlerWrapper<TResponse>
{
    public abstract Task<Result<TResponse>> Handle(object request, IServiceProvider services, CancellationToken ct);
}

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

        var pipelineResult = await pipeline();
        return pipelineResult.Value switch
        {
            Result r => r,
            Failure f => new Result(f),
            _ => throw new UnreachableException(),
        };
    }
}

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

internal abstract class NotificationHandlerWrapper
{
    public abstract Task Handle(object notification, IServiceProvider services, CancellationToken ct);
}

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

public sealed class Mediator(IServiceProvider services) : IMediator
{
    private static readonly ConcurrentDictionary<Type, object> QueryWrappers = new();
    private static readonly ConcurrentDictionary<Type, object> CommandWrappers = new();
    private static readonly ConcurrentDictionary<Type, VoidHandlerWrapper> VoidCommandWrappers = new();
    private static readonly ConcurrentDictionary<Type, NotificationHandlerWrapper> SequentialNotificationWrappers = new();
    private static readonly ConcurrentDictionary<Type, NotificationHandlerWrapper> ParallelNotificationWrappers = new();

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

    public Task<Result> Send(ICommand command, CancellationToken ct = default)
    {
        var wrapper = VoidCommandWrappers.GetOrAdd(
            command.GetType(),
            requestType =>
                (VoidHandlerWrapper)Activator.CreateInstance(typeof(VoidCommandHandlerWrapper<>).MakeGenericType(requestType))!
        );

        return wrapper.Handle(command, services, ct);
    }

    /// <summary>Publishes a notification to all registered handlers using the <see cref="PublishStrategy.Sequential"/> strategy.</summary>
    public Task Publish<TNotification>(TNotification notification, CancellationToken ct = default)
        where TNotification : INotification
    {
        return PublishAsync(notification, PublishStrategy.Sequential, ct);
    }

    /// <summary>Publishes a notification using the specified <paramref name="strategy"/>.</summary>
    /// <param name="strategy"><see cref="PublishStrategy.Sequential"/> runs handlers in order and stops on failure; <see cref="PublishStrategy.Parallel"/> runs all handlers concurrently.</param>
    public Task Publish<TNotification>(TNotification notification, PublishStrategy strategy, CancellationToken ct = default)
        where TNotification : INotification
    {
        return PublishAsync(notification, strategy, ct);
    }

    private Task PublishAsync<TNotification>(TNotification notification, PublishStrategy strategy, CancellationToken ct)
        where TNotification : INotification
    {
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
