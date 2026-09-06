using FluentValidation;
using Interlocutor.Abstractions.Core;
using Interlocutor.Abstractions.Pipeline;
using Interlocutor.AspNetCore;
using Interlocutor.DependencyInjection.Microsoft;
using Interlocutor.Persistence.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TodoTasks.Api.Domain;
using TodoTasks.Api.Features.Tasks.ClearCompletedTodoTasks;
using TodoTasks.Api.Features.Tasks.CompleteTodoTask;
using TodoTasks.Api.Features.Tasks.CreateTodoTask;
using TodoTasks.Api.Features.Tasks.DeleteTodoTask;
using TodoTasks.Api.Features.Tasks.GetTodoTaskById;
using TodoTasks.Api.Features.Tasks.ListTodoTasks;
using TodoTasks.Api.Features.Tasks.ListTodoTasksPaged;
using TodoTasks.Api.Features.Tasks.ReopenTodoTask;
using TodoTasks.Api.Features.Tasks.SearchTodoTasks;
using TodoTasks.Api.Features.Tasks.UpdateTodoTask;
using TodoTasks.Api.Features.Users.CreateUser;
using TodoTasks.Api.Features.Users.DeleteUser;
using TodoTasks.Api.Features.Users.GetUserById;
using TodoTasks.Api.Features.Users.SendWelcomeEmail;
using TodoTasks.Api.Infrastructure;
using TodoTasks.Api.Persistence;

var builder = WebApplication.CreateBuilder(args);

// AspNetCoreRequestContextAccessor (registered below) needs this to read the caller's identity off
// the current HttpContext.
builder.Services.AddHttpContextAccessor();

// DevHeaderAuthenticationHandler - a development/testing stand-in for real authentication, never
// something a real application would ship. See that type's own remarks for the full rationale and
// for why it populates Identity.Name specifically rather than some other claim.
builder
    .Services.AddAuthentication(DevHeaderAuthenticationHandler.SchemeName)
    .AddScheme<AuthenticationSchemeOptions, DevHeaderAuthenticationHandler>(
        DevHeaderAuthenticationHandler.SchemeName,
        _ => { }
    );
builder.Services.AddAuthorization();

// See that type's own remarks - its own try-added default (AmbientRequestContextAccessor) is guarded
// with TryAdd semantics, so registering this one first makes it win. Once a later ticket's
// authentication handler populates HttpContext.User, this accessor reads the caller's id off it
// automatically - nothing bespoke needed on Interlocutor's side.
builder.Services.AddScoped<IRequestContextAccessor, AspNetCoreRequestContextAccessor>();

// A single, kept-open ADO.NET connection to an in-memory SQLite database (Data Source=:memory:),
// registered as a singleton so it stays open for the app's entire lifetime. SQLite discards an
// in-memory database the instant its last connection closes - keeping this one connection object
// alive, and handing that exact same object to every scoped DbContext below (rather than a fresh
// connection per scope), is what lets one request's writes stay visible to the next request's reads.
// See tests/TodoTasks.Api.Tests/Persistence for the smoke test that proves this actually holds across
// scope boundaries, not just within one.
builder.Services.AddSingleton(_ =>
{
    var connection = new SqliteConnection("Data Source=:memory:");
    connection.Open();
    return connection;
});

builder.Services.AddDbContext<TodoTasksDbContext>(
    (services, options) => options.UseSqlite(services.GetRequiredService<SqliteConnection>()),
    contextLifetime: ServiceLifetime.Scoped
);

// Scoped so the whole dispatch - repository reads, IUnitOfWork writes - shares one DbContext instance.
builder.Services.AddScoped(services => new Lazy<TodoTasksDbContext>(services.GetRequiredService<TodoTasksDbContext>));

builder.Services.AddInterlocutorUnitOfWork(services => new EntityFrameworkUnitOfWork<TodoTasksDbContext>(
    services.GetRequiredService<Lazy<TodoTasksDbContext>>()
));

// Each feature registers its own IDomainRepository<TEntity, TKey> here as it lands - see
// documentation/beginners-guide-todo-api.md Step 4 for the shape every one of these follows. Every
// repository added here should be built over the same scoped Lazy<TodoTasksDbContext> as the unit of
// work above, which is the actual mechanism behind "one unit of work, many repositories".
builder.Services.AddInterlocutorRepository<User, int, EntityFrameworkRepository<User, int, TodoTasksDbContext>>(
    services => new EntityFrameworkRepository<User, int, TodoTasksDbContext>(
        services.GetRequiredService<Lazy<TodoTasksDbContext>>(),
        u => u.Id
    )
);

builder.Services.AddInterlocutorRepository<TodoTask, int, EntityFrameworkRepository<TodoTask, int, TodoTasksDbContext>>(
    services => new EntityFrameworkRepository<TodoTask, int, TodoTasksDbContext>(
        services.GetRequiredService<Lazy<TodoTasksDbContext>>(),
        t => t.Id
    )
);

// Singleton, not scoped - counts must persist across requests (and across the multiple scopes each
// request's own dispatch may touch), not reset the moment one request's scope disposes. See
// TodoTaskCompletionCounterHandler, the notification subscriber this backs.
builder.Services.AddSingleton<ICompletionCounter, InMemoryCompletionCounter>();

// Stateless, so a singleton is fine - the same reasoning as ICompletionCounter above doesn't apply here
// (this fake never accumulates anything across requests), but there's no reason to pay for a new
// instance per scope either. Backs SendWelcomeEmailHandler; see IWelcomeMailerClient's own remarks.
builder.Services.AddSingleton<IWelcomeMailerClient, InMemoryWelcomeMailerClient>();

// Registered before AddInterlocutor() below, same reasoning as AspNetCoreRequestContextAccessor above:
// AddAuthorizationBehavior() only try-adds its own default (DefaultPolicyEvaluator, which grants every
// policy), so registering this one first makes it win. Scoped because it depends on the scoped
// IDomainRepository<TodoTask, int> and IRequestContextAccessor.
builder.Services.AddScoped<IPolicyEvaluator, TodoTasksPolicyEvaluator>();

// FluentValidation's own IValidator<T> implementations (CreateUserValidator, and future features'
// equivalents) - not something interlocutor.ScanAssembly below covers. AddValidationBehavior() (called
// below) only wires up ValidationBehavior<,> itself, conditionally, when Interlocutor.FluentValidation is
// referenced - it deliberately leaves *which* validators exist to ordinary FluentValidation registration,
// exactly like this - without it, ValidationBehavior finds zero validators for every request and silently
// skips validation entirely (no error, just nothing enforced), which is worth knowing before you go
// looking for why a validator "isn't running."
builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);

builder.Services.AddInterlocutor(interlocutor =>
{
    interlocutor.ScanAssembly(typeof(Program).Assembly);

    // Logging -> Auditing -> Authorization -> Validation -> Transaction, in that order - see
    // documentation/beginners-guide-todo-api.md Step 2 and documentation/pipeline-and-behaviors.md for
    // the reasoning behind this specific order. This is spelled out as the individual Add*Behavior
    // calls UseRecommendedChain() is sugar for, rather than calling UseRecommendedChain() itself:
    // under Microsoft.Extensions.DependencyInjection specifically, its last step
    // (AddUnitOfWorkBehavior()) throws at registration time once Interlocutor.Persistence.EntityFrameworkCore
    // is referenced, because EF Core's UnitOfWorkBehavior<TRequest,TResponse,TContext> has one more open
    // type parameter than Microsoft DI allows an open-generic registration to carry. See
    // TodoTasksUnitOfWorkBehavior's own remarks, and documentation/transactions-and-store-integration.md's
    // "EntityFramework Core Integration" section, for the closed-subclass workaround registered here via
    // the general-purpose AddBehavior escape hatch instead.
    interlocutor
        .Pipeline.AddLoggingBehavior()
        .AddAuditingBehavior()
        .AddAuthorizationBehavior()
        .AddValidationBehavior()
        .AddBehavior(typeof(TodoTasksUnitOfWorkBehavior<,>))
        // A second custom behavior beyond the recommended chain - see its own remarks for why
        // "innermost, registered last" is what makes it measure only the handler's own work.
        .AddBehavior(typeof(SlowHandlerWarningBehavior<,>))
        // Teaches the pipeline's IOutcomeCaseRegistry to construct SendWelcomeEmailResult's cases -
        // Interlocutor's own worked example of a hand-rolled IUnion response type, per
        // documentation/messages-and-result-types.md's "IUnion as an Extension Point" section. Falls back
        // to DefaultOutcomeCaseRegistry for every other response type this project uses (every
        // compiler-generated Outcome<...> arity), so this doesn't disturb any existing feature's
        // construction/extraction - see SendWelcomeEmailResultCaseRegistry's own remarks.
        .OverrideOutcomeCaseRegistry<SendWelcomeEmailResultCaseRegistry>();
});

var app = builder.Build();

// Must run before any endpoint that calls RequireAuthorization() - this is what actually invokes
// DevHeaderAuthenticationHandler and populates HttpContext.User for the rest of the pipeline.
app.UseAuthentication();
app.UseAuthorization();

// There are no migrations for this proof-of-concept - the schema is (re)created from the current
// DbContext model every time the app starts, against the one kept-open connection above so it's
// visible to every DbContext resolved afterward against that same in-memory database.
await using (var scope = app.Services.CreateAsyncScope())
{
    await scope.ServiceProvider.GetRequiredService<TodoTasksDbContext>().Database.EnsureCreatedAsync().ConfigureAwait(false);
}

// A trivially cheap liveness check - also doubles as proof, for TodoTasks.Api.Tests' HTTP fixture,
// that the whole host (DI container, EF Core schema creation, pipeline registration) came up cleanly
// before any feature endpoint exists to exercise instead.
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

// A small, permanently-useful diagnostic twin to /health: proves the X-User-Id header actually
// authenticates end to end (dev-auth handler -> AspNetCoreRequestContextAccessor -> here), without
// needing a real feature endpoint to exercise the same path.
app.MapGet(
        "/health/whoami",
        (IRequestContextAccessor contextAccessor) => Results.Ok(new { userId = contextAccessor.Context?.UserId })
    )
    .RequireAuthorization();

// One line per feature endpoint, added as each feature lands - see each Features/**/Map*Endpoint.cs.
app.MapCreateUserEndpoint();
app.MapGetUserByIdEndpoint();
app.MapDeleteUserEndpoint();
app.MapSendWelcomeEmailEndpoint();
app.MapCreateTodoTaskEndpoint();
app.MapGetTodoTaskByIdEndpoint();
app.MapListTodoTasksEndpoint();
app.MapListTodoTasksPagedEndpoint();
app.MapSearchTodoTasksEndpoint();
app.MapUpdateTodoTaskEndpoint();
app.MapCompleteTodoTaskEndpoint();
app.MapReopenTodoTaskEndpoint();
app.MapDeleteTodoTaskEndpoint();
app.MapClearCompletedTodoTasksEndpoint();

await app.RunAsync().ConfigureAwait(false);

/// <summary>
/// Exposes this file's top-level-statements entry point as a named type so
/// <c>WebApplicationFactory&lt;Program&gt;</c> (used throughout <c>TodoTasks.Api.Tests</c>' HTTP
/// integration tests) can host this application in-process.
/// </summary>
/// <remarks>
/// Not <see langword="static"/>: <c>WebApplicationFactory&lt;TEntryPoint&gt;</c> requires its type
/// argument to be an instantiable class, so this type must stay non-static. The explicit
/// <see langword="protected"/> constructor instead blocks direct external instantiation while
/// still satisfying that generic constraint.
/// </remarks>
public partial class Program
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Program"/> class. Not callable from outside this
    /// assembly or a derived type - this class exists only as a type marker for
    /// <c>WebApplicationFactory&lt;Program&gt;</c>.
    /// </summary>
    protected Program() { }
}
