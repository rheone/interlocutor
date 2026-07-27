using FluentValidation;
using Mediance;
using Mediance.Mediation;
using Mediance.Mediation.FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Mediance.Example.Orders;

// ---------------------------------------------------------------------------
// Everything the LIBRARY provides is plumbing: Result<T>, the pipeline, the
// marker interfaces, the mediator dispatch. Everything below is YOURS — the
// query/command shape, the validation rules, and the handler body are all
// application code you write and register. Nothing in Mediance.* ever
// guesses at business logic on your behalf; it only calls the pieces you
// supplied, in the order you registered them.
// ---------------------------------------------------------------------------

public sealed class Order
{
    public Guid Id { get; init; }
    public required string Status { get; set; } // "Pending" | "Cancelled" | "Shipped"
    public required string CustomerUserId { get; init; }
}

public sealed record OrderDto(Guid Id, string Status);

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();
}

public interface ICurrentUser
{
    string UserId { get; }
}

// ---------------------------------------------------------------------------
// EXAMPLE 1 — a query
// ---------------------------------------------------------------------------

/// <summary>The shape of the request. You choose the fields.</summary>
public sealed record GetOrderByIdQuery(Guid OrderId) : IQuery<OrderDto>;

/// <summary>
/// YOUR validation rule. ValidationBehavior (library) discovers and runs
/// this automatically because it's registered as IValidator&lt;GetOrderByIdQuery&gt;
/// — the library never invents the rule itself.
/// </summary>
public sealed class GetOrderByIdQueryValidator : AbstractValidator<GetOrderByIdQuery>
{
    public GetOrderByIdQueryValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
    }
}

/// <summary>
/// YOUR query logic. The library calls this exactly once, after validation
/// has already passed — everything inside the method body is entirely your
/// decision: which store to query, what NotFound means for this query,
/// what shape to project into.
/// </summary>
public sealed class GetOrderByIdQueryHandler(AppDbContext db) : IQueryHandler<GetOrderByIdQuery, OrderDto>
{
    public async Task<Result<OrderDto>> Handle(GetOrderByIdQuery query, CancellationToken ct)
    {
        var order = await db.Orders.AsNoTracking().FirstOrDefaultAsync(o => o.Id == query.OrderId, ct);

        // <-- your logic: what counts as "not found" for this query
        if (order is null)
            return new NotFound(nameof(Order), query.OrderId);

        // <-- your logic: what shape to return
        return new OrderDto(order.Id, order.Status);
    }
}

// ---------------------------------------------------------------------------
// EXAMPLE 2 — a command
// ---------------------------------------------------------------------------

/// <summary>The shape of the request, plus the marker interfaces YOU chose for it.</summary>
public sealed record CancelOrderCommand(Guid OrderId) : ICommand<OrderDto>, ITransactionalRequest, IAuditableRequest
{
    public string ActionName => "Order.Cancel"; // <-- your label, used by AuditingBehavior
}

/// <summary>YOUR shape validation — trivial here, but this is where field-level rules for this command live.</summary>
public sealed class CancelOrderCommandValidator : AbstractValidator<CancelOrderCommand>
{
    public CancelOrderCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
    }
}

/// <summary>
/// YOUR resource lookup, ownership check, and business rule — the three
/// abstract methods below are the entirety of what you write.
/// ResourceCommandHandler.Handle (library, not shown here) just calls them
/// in sequence: LoadAsync -&gt; Authorize -&gt; HandleAsync.
/// </summary>
public sealed class CancelOrderCommandHandler(AppDbContext db, ICurrentUser currentUser)
    : ResourceCommandHandler<CancelOrderCommand, Order, OrderDto>
{
    /// <summary>YOUR lookup. Returning null here is what turns into NotFound — you decide the query.</summary>
    protected override Task<Order?> LoadAsync(CancelOrderCommand command, CancellationToken ct) =>
        db.Orders.FirstOrDefaultAsync(o => o.Id == command.OrderId, ct);

    protected override object DescribeKey(CancelOrderCommand command) => command.OrderId;

    /// <summary>YOUR authorization rule. The library has no opinion on what "allowed" means for an order.</summary>
    protected override Failure? Authorize(Order entity, CancelOrderCommand command) =>
        entity.CustomerUserId == currentUser.UserId ? null : new Forbidden($"User does not own order {entity.Id}.");

    /// <summary>YOUR business rule: an already-shipped order can't be cancelled — this is a RuleViolation, not a ValidationFailed, because it depends on state the handler just loaded.</summary>
    protected override Task<Result<OrderDto>> HandleAsync(CancelOrderCommand command, Order entity, CancellationToken ct)
    {
        if (entity.Status == "Shipped")
        {
            return Task.FromResult<Result<OrderDto>>(
                new RuleViolation("order.already_shipped", "A shipped order cannot be cancelled.")
            );
        }

        entity.Status = "Cancelled";
        // No SaveChangesAsync here — TransactionBehavior (library) commits
        // once the whole pipeline returns a non-Failure Result, because this
        // command implements ITransactionalRequest.
        return Task.FromResult<Result<OrderDto>>(new OrderDto(entity.Id, entity.Status));
    }
}

// ---------------------------------------------------------------------------
// Registration — this is the only place you tell the container which
// classes implement which contracts. Everything above this line is your
// logic; everything below is wiring it into the library's pipeline.
// ---------------------------------------------------------------------------

public static class OrderCompositionRoot
{
    public static IServiceCollection AddOrderExamples(this IServiceCollection services)
    {
        services.AddScoped<IQueryHandler<GetOrderByIdQuery, OrderDto>, GetOrderByIdQueryHandler>();
        services.AddScoped<IValidator<GetOrderByIdQuery>, GetOrderByIdQueryValidator>();

        services.AddScoped<ICommandHandler<CancelOrderCommand, OrderDto>, CancelOrderCommandHandler>();
        services.AddScoped<IValidator<CancelOrderCommand>, CancelOrderCommandValidator>();

        return services;
        // Pipeline behaviors (Logging, Validation, Transaction, ...) are
        // registered once, application-wide, per the earlier composition
        // root — nothing query/command-specific is needed there. Adding a
        // new query or command never touches pipeline registration at all.
    }
}
