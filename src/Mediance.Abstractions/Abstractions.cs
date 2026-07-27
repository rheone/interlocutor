using Mediance.CommonKernel;

namespace Mediance.Abstractions;

public interface IQuery<TResponse>;

public interface IEnumerableQuery<TItem> : IQuery<IReadOnlyList<TItem>>;

public interface IPagedQuery<TItem> : IQuery<PagedResult<TItem>>
{
    int Page { get; }

    int PageSize { get; }
}

public interface ICommand<TResponse>;

public interface ICommand;

public interface IEnumerableCommand<TResponseItem> : ICommand<IReadOnlyList<TResponseItem>>;

public interface IBulkCommand<TItem, TResponseItem> : ICommand<IReadOnlyList<TResponseItem>>
{
    IReadOnlyList<TItem> Items { get; }
}

public interface ITransactionalRequest;

public interface IExternalStoreRequest;

public interface IAuditableRequest
{
    string ActionName { get; }
}

public interface IRequiresPolicy
{
    string PolicyName { get; }
}

public interface IQueryHandler<in TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
    Task<Result<TResponse>> Handle(TQuery query, CancellationToken ct);
}

public interface ICommandHandler<in TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
    Task<Result<TResponse>> Handle(TCommand command, CancellationToken ct);
}

public interface ICommandHandler<in TCommand>
    where TCommand : ICommand
{
    Task<Result> Handle(TCommand command, CancellationToken ct);
}

public interface IPipelineBehavior<TRequest, TResponse>
{
    Task<Result<TResponse>> Handle(TRequest request, Func<Task<Result<TResponse>>> next, CancellationToken ct);
}

public interface IMediator
{
    Task<Result<TResponse>> Send<TResponse>(IQuery<TResponse> query, CancellationToken ct = default);

    Task<Result<TResponse>> Send<TResponse>(ICommand<TResponse> command, CancellationToken ct = default);

    Task<Result> Send(ICommand command, CancellationToken ct = default);
}

public abstract class ResourceCommandHandler<TCommand, TEntity, TResponse>
    where TCommand : ICommand<TResponse>
{
    protected abstract Task<TEntity?> LoadAsync(TCommand command, CancellationToken ct);

    protected abstract Failure? Authorize(TEntity entity, TCommand command);

    protected abstract Task<Result<TResponse>> HandleAsync(TCommand command, TEntity entity, CancellationToken ct);

    public async Task<Result<TResponse>> Handle(TCommand command, CancellationToken ct)
    {
        var entity = await LoadAsync(command, ct);
        if (entity is null)
        {
            return new Result<TResponse>(new NotFound(typeof(TEntity).Name, DescribeKey(command)));
        }

        var authorizationResult = Authorize(entity, command);

        if (authorizationResult is not null)
        {
            return new Result<TResponse>(authorizationResult.Value);
        }

        return await HandleAsync(command, entity, ct);
    }

    protected virtual object DescribeKey(TCommand command) => command!;
}
