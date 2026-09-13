using Interlocutor;

namespace Interlocutor.Mediation;

// ---------------------------------------------------------------------------
// Package: Interlocutor.Mediation
// Depends only on Interlocutor (the kernel). Everything here is storage- and
// framework-agnostic - no EF Core, no ASP.NET, no FluentValidation. Those
// bindings live in the satellite packages that reference this one.
// ---------------------------------------------------------------------------

/// <summary>A read request returning a single <typeparamref name="TResponse"/>.</summary>
public interface IQuery<TResponse>;

/// <summary>
/// A read request returning a materialized collection of
/// <typeparamref name="TItem"/>. A thin specialization of
/// <see cref="IQuery{TResponse}"/> so a query author can write
/// <c>IEnumerableQuery&lt;WidgetDto&gt;</c> instead of
/// <c>IQuery&lt;IReadOnlyList&lt;WidgetDto&gt;&gt;</c> - purely a
/// readability aid, no behavior is keyed off this interface specifically.
/// </summary>
public interface IEnumerableQuery<TItem> : IQuery<IReadOnlyList<TItem>>;

/// <summary>
/// A read request returning a page of <typeparamref name="TItem"/>. Unlike
/// <see cref="IEnumerableQuery{TItem}"/>, this interface's members
/// (<see cref="Page"/>, <see cref="PageSize"/>) are load-bearing: the
/// library's <c>PagingValidationBehavior</c> validates any request
/// implementing this interface without a hand-written validator.
/// </summary>
public interface IPagedQuery<TItem> : IQuery<PagedResult<TItem>>
{
    /// <summary>1-based page number requested.</summary>
    int Page { get; }

    /// <summary>Items requested per page.</summary>
    int PageSize { get; }
}

/// <summary>A write request returning a single <typeparamref name="TResponse"/>.</summary>
public interface ICommand<TResponse>;

/// <summary>A write request with no return payload.</summary>
public interface ICommand;

/// <summary>
/// A write request that returns a materialized collection of
/// <typeparamref name="TResponseItem"/>. Readability aid, no behavior keyed
/// off it specifically - contrast with <see cref="IBulkCommand{TItem,TResponseItem}"/>.
/// </summary>
public interface IEnumerableCommand<TResponseItem> : ICommand<IReadOnlyList<TResponseItem>>;

/// <summary>
/// A write request that creates/acts on many <typeparamref name="TItem"/>
/// at once and returns one <typeparamref name="TResponseItem"/> per input
/// item. <see cref="Items"/> is load-bearing: the library's
/// <c>BulkItemValidationBehavior</c> resolves <c>IValidator&lt;TItem&gt;</c>
/// and validates every item automatically for any request implementing
/// this interface - no per-command <c>RuleForEach</c> wrapper required
/// unless cross-item rules are needed.
/// </summary>
public interface IBulkCommand<TItem, TResponseItem> : ICommand<IReadOnlyList<TResponseItem>>
{
    IReadOnlyList<TItem> Items { get; }
}

/// <summary>
/// Opts a command into the unit-of-work provided by a persistence satellite
/// package (e.g. <c>Interlocutor.Mediation.EntityFrameworkCore</c>'s
/// <c>TransactionBehavior</c>). Every concrete command should implement
/// exactly one of this or <see cref="IExternalStoreRequest"/> - enforced by
/// an architecture test, not the compiler.
/// </summary>
public interface ITransactionalRequest;

/// <summary>
/// Declares that a command is deliberately backed by a non-relational store
/// or external API. A correctly written transaction behavior must never
/// resolve a database session for a request carrying only this marker.
/// </summary>
public interface IExternalStoreRequest;

/// <summary>Opts a request into audit logging; a no-op for any request that doesn't implement this.</summary>
public interface IAuditableRequest
{
    string ActionName { get; }
}

/// <summary>Declares a named, non-resource authorization policy that must pass before the handler runs.</summary>
public interface IRequiresPolicyRequest
{
    string PolicyName { get; }
}

/// <summary>Handles a query and produces its result.</summary>
public interface IQueryHandler<in TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
    Task<Result<TResponse>> Handle(TQuery query, CancellationToken ct);
}

/// <summary>Handles a command that returns a value.</summary>
public interface ICommandHandler<in TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
    Task<Result<TResponse>> Handle(TCommand command, CancellationToken ct);
}

/// <summary>Handles a command with no return payload.</summary>
public interface ICommandHandler<in TCommand>
    where TCommand : ICommand
{
    Task<Result> Handle(TCommand command, CancellationToken ct);
}

/// <summary>
/// A single cross-cutting concern wrapping the next stage of the pipeline
/// (onion / Russian-doll model). Implementations are generic over
/// <typeparamref name="TResponse"/> and must never inspect it, so the same
/// behavior applies uniformly to scalar, enumerable, paged, and bulk
/// response shapes alike.
/// </summary>
public interface IPipelineBehavior<TRequest, TResponse>
{
    Task<Result<TResponse>> Handle(TRequest request, CancellationToken ct, Func<Task<Result<TResponse>>> next);
}

/// <summary>
/// The single public dispatch entry point. The only place a default
/// <see cref="CancellationToken"/> value is offered - every internal layer
/// requires it explicitly.
/// </summary>
public interface IMediator
{
    Task<Result<TResponse>> Send<TResponse>(IQuery<TResponse> query, CancellationToken ct = default);
    Task<Result<TResponse>> Send<TResponse>(ICommand<TResponse> command, CancellationToken ct = default);
    Task<Result> Send(ICommand command, CancellationToken ct = default);
}

/// <summary>
/// Template-method base for commands that act on a single, previously
/// persisted resource and require resource-based authorization (checks
/// that depend on the loaded entity, such as ownership). Deliberately not a
/// pipeline behavior - see the library README §"Why resource authorization
/// isn't a behavior" - because passing the loaded entity between generic
/// behaviors would require shared mutable state.
/// </summary>
public abstract class ResourceCommandHandler<TCommand, TEntity, TResponse>
    where TCommand : ICommand<TResponse>
{
    protected abstract Task<TEntity?> LoadAsync(TCommand command, CancellationToken ct);
    protected abstract Failure? Authorize(TEntity entity, TCommand command);
    protected abstract Task<Result<TResponse>> HandleAsync(TCommand command, TEntity entity, CancellationToken ct);

    /// <summary>Runs the fixed lookup -&gt; authorize -&gt; handle sequence. Not overridden by concrete handlers.</summary>
    public async Task<Result<TResponse>> Handle(TCommand command, CancellationToken ct)
    {
        var entity = await LoadAsync(command, ct);
        if (entity is null)
            return new NotFound(typeof(TEntity).Name, DescribeKey(command));

        if (Authorize(entity, command) is { } denied)
            return denied;

        return await HandleAsync(command, entity, ct);
    }

    /// <summary>Override to describe the key being looked up, for the <see cref="NotFound"/> diagnostic payload.</summary>
    protected virtual object DescribeKey(TCommand command) => command!;
}
