using FluentValidation;
using Interlocutor;
using Interlocutor.Mediation;
using Interlocutor.Mediation.EntityFrameworkCore;
using Interlocutor.Mediation.FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Interlocutor.Example;

// ---------------------------------------------------------------------------
// Everything below is application code - NOT part of any Interlocutor
// package - showing how a consumer wires the library together and uses the
// IPagedQuery<T> / IBulkCommand<TItem,TResponseItem> variants.
// ---------------------------------------------------------------------------

public sealed class Widget
{
    public Guid Id { get; init; }
    public required string Name { get; set; }
    public int Quantity { get; set; }
    public required string OwnerUserId { get; init; }
}

public sealed record WidgetDto(Guid Id, string Name, int Quantity);

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Widget> Widgets => Set<Widget>();
}

/// <summary>
/// A paged query using the library's <see cref="IPagedQuery{TItem}"/>
/// contract. Implementing <c>Page</c>/<c>PageSize</c> is all that's needed
/// for <see cref="PagingValidationBehavior{TRequest,TItem}"/> to validate
/// this query automatically - no <c>GetWidgetsQueryValidator</c> is
/// required unless additional, application-specific rules are wanted.
/// </summary>
public sealed record GetWidgetsQuery(int Page, int PageSize, string? SearchTerm) : IPagedQuery<WidgetDto>;

public sealed class GetWidgetsQueryHandler(AppDbContext db) : IQueryHandler<GetWidgetsQuery, PagedResult<WidgetDto>>
{
    public async Task<Result<PagedResult<WidgetDto>>> Handle(GetWidgetsQuery query, CancellationToken ct)
    {
        var baseQuery = db.Widgets.AsNoTracking().Where(w => query.SearchTerm == null || w.Name.Contains(query.SearchTerm));

        var total = await baseQuery.LongCountAsync(ct);
        var items = await baseQuery
            .OrderBy(w => w.Name)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(w => new WidgetDto(w.Id, w.Name, w.Quantity))
            .ToListAsync(ct);

        return new PagedResult<WidgetDto>(items, query.Page, query.PageSize, total);
    }
}

public sealed record CreateWidgetItem(string Name, int Quantity);

/// <summary>
/// A bulk command using the library's <see cref="IBulkCommand{TItem,TResponseItem}"/>
/// contract. Implementing <c>Items</c> is all that's needed for
/// <see cref="BulkItemValidationBehavior{TRequest,TItem,TResponseItem}"/>
/// to validate every item automatically via <c>IValidator&lt;CreateWidgetItem&gt;</c>
/// - no hand-written <c>RuleForEach</c> wrapper validator required.
/// </summary>
public sealed record BulkCreateWidgetsCommand(IReadOnlyList<CreateWidgetItem> Items)
    : IBulkCommand<CreateWidgetItem, WidgetDto>,
        ITransactionalRequest,
        IAuditableRequest
{
    public string ActionName => "Widget.BulkCreate";
}

public sealed class CreateWidgetItemValidator : AbstractValidator<CreateWidgetItem>
{
    public CreateWidgetItemValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Quantity).GreaterThanOrEqualTo(0);
    }
}

public sealed class BulkCreateWidgetsCommandHandler(AppDbContext db, ICurrentUser currentUser)
    : ICommandHandler<BulkCreateWidgetsCommand, IReadOnlyList<WidgetDto>>
{
    public Task<Result<IReadOnlyList<WidgetDto>>> Handle(BulkCreateWidgetsCommand command, CancellationToken ct)
    {
        var created = new List<WidgetDto>(command.Items.Count);
        foreach (var item in command.Items)
        {
            var widget = new Widget
            {
                Id = Guid.NewGuid(),
                Name = item.Name,
                Quantity = item.Quantity,
                OwnerUserId = currentUser.UserId,
            };
            db.Widgets.Add(widget);
            created.Add(new WidgetDto(widget.Id, widget.Name, widget.Quantity));
        }
        return Task.FromResult<Result<IReadOnlyList<WidgetDto>>>(created);
    }
}

public interface ICurrentUser
{
    string UserId { get; }
}

/// <summary>
/// Composition root. Registration order for the open-generic
/// <c>IPipelineBehavior&lt;,&gt;</c> entries determines pipeline order -
/// this is what <see cref="Mediator.Dispatch{TResponse}"/> folds into the
/// nested "Russian doll" call graph at dispatch time.
/// </summary>
public static class CompositionRoot
{
    public static IServiceCollection AddInterlocutorExample(this IServiceCollection services)
    {
        services.AddScoped<IMediator, Mediator>();
        services.AddScoped<AppDbContext>();
        services.AddScoped(sp => new Lazy<AppDbContext>(sp.GetRequiredService<AppDbContext>));

        services.AddScoped<IQueryHandler<GetWidgetsQuery, PagedResult<WidgetDto>>, GetWidgetsQueryHandler>();
        services.AddScoped<
            ICommandHandler<BulkCreateWidgetsCommand, IReadOnlyList<WidgetDto>>,
            BulkCreateWidgetsCommandHandler
        >();
        services.AddScoped<IValidator<CreateWidgetItem>, CreateWidgetItemValidator>();

        // Pipeline order (outer -> inner). This list IS the "nesting doll"
        // - see Mediator.Dispatch for how it's folded into nested calls.
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(AuditingBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        // Registered generically for any TRequest : IPagedQuery<TItem> / IBulkCommand<TItem,TResponseItem>;
        // DI only closes these generics for request types that actually satisfy the constraint.
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(PagingValidationBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(BulkItemValidationBehavior<,,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(AuthorizationBehavior<,>));
        // TransactionBehavior<TRequest,TResponse,TContext> takes an extra type
        // parameter (TContext) versus IPipelineBehavior<TRequest,TResponse>, so
        // it can't be registered against the open IPipelineBehavior<,> contract
        // directly. AppWidgetTransactionBehavior below closes TContext to this
        // app's AppDbContext so it fits the open-generic registration pattern.
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(AppWidgetTransactionBehavior<,>));

        return services;
    }
}

/// <summary>
/// Closes the library's <see cref="TransactionBehavior{TRequest,TResponse,TContext}"/>
/// against this application's <see cref="AppDbContext"/>, so it can be
/// registered against the two-parameter <see cref="IPipelineBehavior{TRequest,TResponse}"/>
/// contract the rest of the pipeline expects. This one-line adapter is the
/// only application-specific glue needed to use the EF Core satellite.
/// </summary>
public sealed class AppWidgetTransactionBehavior<TRequest, TResponse>(Lazy<AppDbContext> db)
    : TransactionBehavior<TRequest, TResponse, AppDbContext>(db);

/*
 * ---------------------------------------------------------------------------
 * WORKED EXAMPLE - tracing BulkCreateWidgetsCommand end to end
 * ---------------------------------------------------------------------------
 * Given the registration order above, Mediator.Dispatch builds this nested
 * pipeline for a BulkCreateWidgetsCommand (folding, per Mediator.cs, so the
 * FIRST-registered behavior becomes the OUTERMOST call):
 *
 *   Logging(next: Auditing(next: Validation(next: BulkItemValidation(next:
 *     Authorization(next: Transaction(next: Handler)))))
 *
 * Caller:
 *   var command = new BulkCreateWidgetsCommand([new("Bolt", 100), new("", -1)]);
 *   var result  = await mediator.Send(command, ct);
 *   return result.ToActionResult();
 *
 * Trace:
 *   1. Logging       - logs "Handling BulkCreateWidgetsCommand", starts a stopwatch, calls next().
 *   2. Auditing       - command implements IAuditableRequest, so it calls next()
 *                        and will record ActionName="Widget.BulkCreate" with the eventual outcome.
 *   3. Validation     - no IValidator<BulkCreateWidgetsCommand> is registered
 *                        (no cross-item rules needed here), so this is a no-op; calls next().
 *   4. BulkItemValidation - command implements IBulkCommand<CreateWidgetItem,WidgetDto>;
 *                        validates each item via IValidator<CreateWidgetItem>.
 *                        Item[1] ("", -1) fails both NotEmpty and GreaterThanOrEqualTo(0).
 *                        SHORT-CIRCUITS here: returns
 *                        ValidationFailed({ "Items[1].Name": [...], "Items[1].Quantity": [...] })
 *                        - Authorization, Transaction, and the handler never run.
 *   5. (not reached)  Authorization, Transaction, Handler.
 *   6. Back up the stack: Validation, Auditing (records ActionName with succeeded=false),
 *      Logging (logs "Handled BulkCreateWidgetsCommand in Nms -> ValidationFailed") each
 *      just return the same Result unchanged.
 *   7. Controller: result.ToActionResult() matches Failure { Value: ValidationFailed }
 *      and returns 400 Bad Request with a ValidationProblemDetails body.
 *
 * With a fully valid Items list instead, step 4 calls next(): Authorization
 * is a no-op (command doesn't implement IRequiresPolicy), Transaction resolves
 * db.Value (first time - this command IS ITransactionalRequest), begins a
 * transaction, calls the Handler, which adds N Widget entities and returns
 * Result<IReadOnlyList<WidgetDto>> with the created DTOs. Transaction sees a
 * non-Failure Result, calls SaveChangesAsync + CommitAsync once, and the
 * DTOs flow back up through every behavior unchanged to the controller,
 * which returns 200 OK with the list of WidgetDto - the actual data the
 * caller asked for, having passed through five behaviors without any of
 * them needing to know what a Widget is.
 * ---------------------------------------------------------------------------
 */
