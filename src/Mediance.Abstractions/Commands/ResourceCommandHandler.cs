using Mediance.CommonKernel;

namespace Mediance.Abstractions.Commands;

public abstract class ResourceCommandHandler<TCommand, TEntity, TResponse>
    where TCommand : ICommand<TResponse>
{
    protected abstract Task<TEntity?> LoadAsync(TCommand command, CancellationToken ct);

    protected abstract Failure? Authorize(TEntity entity, TCommand command);

    protected abstract Task<Result<TResponse>> HandleAsync(TCommand command, TEntity entity, CancellationToken ct);

    public async Task<Result<TResponse>> Handle(TCommand command, CancellationToken ct)
    {
        var entity = await this.LoadAsync(command, ct);
        if (entity is null)
        {
            return new Result<TResponse>(new NotFound(typeof(TEntity).Name, this.DescribeKey(command)));
        }

        var authorizationResult = this.Authorize(entity, command);

        if (authorizationResult is not null)
        {
            return new Result<TResponse>(authorizationResult.Value);
        }

        return await this.HandleAsync(command, entity, ct);
    }

    protected virtual object DescribeKey(TCommand command) => command!;
}
