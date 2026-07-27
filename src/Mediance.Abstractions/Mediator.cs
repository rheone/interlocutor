using Mediance.CommonKernel;

namespace Mediance.Abstractions;

internal abstract class HandlerWrapper<TResponse>
{
    public abstract Task<Result<TResponse>> Handle(object request, IServiceProvider services, CancellationToken ct);
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

public sealed class Mediator(IServiceProvider services) : IMediator
{
    private static readonly ConcurrentDictionary<Type, object> QueryWrappers = new();
    private static readonly ConcurrentDictionary<Type, object> CommandWrappers = new();

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

    public async Task<Result> Send(ICommand command, CancellationToken ct = default)
    {
        var handlerType = typeof(ICommandHandler<>).MakeGenericType(command.GetType());
        var handler = services.GetRequiredService(handlerType);
        var handleMethod = handlerType.GetMethod(nameof(ICommandHandler<ICommand>.Handle))!;
        return await (Task<Result>)handleMethod.Invoke(handler, [command, ct])!;
    }
}
