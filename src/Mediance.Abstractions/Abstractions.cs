using Mediance.CommonKernel;

namespace Mediance.Abstractions;

/// <summary>Marker interface for domain events or notifications. Has no return value — fire-and-forget.</summary>
public interface INotification;

/// <summary>Handles a single notification type. Multiple handlers may register for the same <typeparamref name="TNotification"/>.</summary>
/// <typeparam name="TNotification">The notification type this handler consumes.</typeparam>
public interface INotificationHandler<in TNotification>
    where TNotification : INotification
{
    /// <summary>Processes the notification.</summary>
    Task Handle(TNotification notification, CancellationToken ct);
}

/// <summary>Send-only interface for request/response operations. Exposes <see cref="Send{TResponse}(IQuery{TResponse},CancellationToken)"/> for queries and commands.</summary>
public interface ISender
{
    /// <summary>Dispatches a query to its registered handler, returning the result through all pipeline behaviors.</summary>
    Task<Result<TResponse>> Send<TResponse>(IQuery<TResponse> query, CancellationToken ct = default);

    /// <summary>Dispatches a value-returning command to its registered handler, returning the result through all pipeline behaviors.</summary>
    Task<Result<TResponse>> Send<TResponse>(ICommand<TResponse> command, CancellationToken ct = default);

    /// <summary>Dispatches a void command to its registered handler, returning the result through all pipeline behaviors.</summary>
    Task<Result> Send(ICommand command, CancellationToken ct = default);
}

/// <summary>Publish-only interface for fire-and-forget notifications. Exposes <see cref="Publish{TNotification}(TNotification,CancellationToken)"/>.</summary>
public interface IPublisher
{
    /// <summary>Publishes a notification to all registered <see cref="INotificationHandler{TNotification}"/> instances.</summary>
    Task Publish<TNotification>(TNotification notification, CancellationToken ct = default)
        where TNotification : INotification;
}

/// <summary>Scoped ambient context carrying correlation ID, user identity, and tenant information through the request pipeline.</summary>
/// <remarks>Null-safe — if no middleware populates this, <see cref="Context"/> is null. Consumers must handle gracefully.</remarks>
public interface IRequestContextAccessor
{
    /// <summary>Gets or sets the current request context. May be null if not populated.</summary>
    RequestContext? Context { get; set; }
}

/// <summary>Immutable snapshot of request-scoped ambient data.</summary>
public sealed record RequestContext
{
    /// <summary>Gets a unique identifier for correlating this request across services. Auto-generated if not set.</summary>
    public string CorrelationId { get; init; } = Guid.NewGuid().ToString();

    /// <summary>Gets the authenticated user identifier, or null if anonymous.</summary>
    public string? UserId { get; init; }

    /// <summary>Gets the tenant identifier for multi-tenant scenarios, or null if not scoped.</summary>
    public string? TenantId { get; init; }

    /// <summary>Gets an arbitrary key-value bag for extension data, or null if not set.</summary>
    public IReadOnlyDictionary<string, object>? Items { get; init; }
}

/// <summary>Controls how notifications are dispatched to multiple handlers.</summary>
public enum PublishStrategy
{
    /// <summary>Handlers execute in registration order. Stops on the first exception.</summary>
    Sequential,

    /// <summary>Handlers execute concurrently via Task.WhenAll. Exceptions are aggregated.</summary>
    Parallel,
}

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

public interface IMediator : ISender, IPublisher;

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
