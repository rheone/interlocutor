using Autofac;
using Autofac.Extensions.DependencyInjection;
using FluentValidation;
using Interlocutor.Abstractions.Core;
using Interlocutor.Abstractions.Pipeline;
using Interlocutor.Abstractions.Pipeline.Classifiers;
using Interlocutor.AspNetCore;
using Interlocutor.DependencyInjection.Autofac;
using Interlocutor.Persistence.NHibernate;
using Microsoft.AspNetCore.Authentication;
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
using TodoTasks.Api.Infrastructure;
using TodoTasks.Api.Persistence;
// Microsoft.NET.Sdk.Web's implicit usings bring in Microsoft.AspNetCore.Http, which has its own
// ISession (the HTTP session-state abstraction) - completely unrelated to NHibernate's ISession
// this project actually means everywhere below. This alias makes "ISession" mean NHibernate's
// unambiguously for the rest of this file, without qualifying every single use of it.
using ISession = NHibernate.ISession;
using ISessionFactory = NHibernate.ISessionFactory;

var builder = WebApplication.CreateBuilder(args);

// Host the app on an Autofac container instead of the built-in Microsoft.Extensions.DependencyInjection
// one - see documentation/beginners-guide-todo-api.md Step 2 for the full reasoning. Interlocutor itself
// works identically on either container; this project follows the guide's own choice.
builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());

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
        options => { }
    );
builder.Services.AddAuthorization();

// TodoTasksController/UsersController - the MVC-controller twin of this project's minimal-API surface
// (see those types' own remarks). AddControllersAsServices() routes controller activation through the
// same Autofac container as every other registration below, instead of Microsoft.Extensions.DI's default
// activator - without it, constructor injection of ISender/IRequestContextAccessor into a controller
// would still work (the request IServiceProvider is Autofac-backed either way), but the controller type
// itself wouldn't be a first-class Autofac registration, which matters if a later feature wants to
// intercept or decorate it the way this project already can for anything else in the container.
builder.Services.AddControllers().AddControllersAsServices();

// Built once, before the container exists, exactly like documentation/beginners-guide-todo-api.md
// Step 4/10 build their own session factory above ConfigureContainer - see TodoTasksSessionFactory's
// own remarks for the shared-cache-mode mechanism that keeps this project's in-memory database alive
// across requests (and for why the more "obvious" single-shared-connection approach doesn't work).
var (anchorConnection, sessionFactory) = TodoTasksSessionFactory.Build();

builder.Host.ConfigureContainer<ContainerBuilder>(containerBuilder =>
{
    // Register this before AddInterlocutor - its own try-added default (AmbientRequestContextAccessor)
    // is guarded with Autofac's IfNotRegistered, so registering this one first makes it win. (The
    // startup "Interlocutor registration report" log still lists AmbientRequestContextAccessor as a
    // "[Default]" - that log only describes what AddInterlocutor *offered* to try-add, not which
    // registration actually won; verified at runtime, not just read from the log.) Once ticket 04's
    // authentication handler populates HttpContext.User, this accessor reads the caller's id off it
    // automatically - nothing bespoke needed on Interlocutor's side.
    containerBuilder
        .RegisterType<AspNetCoreRequestContextAccessor>()
        .As<IRequestContextAccessor>()
        .InstancePerLifetimeScope();

    // The anchor connection is registered purely so its lifetime (and disposal at shutdown) is managed
    // by the container - nothing resolves it to build a session with. See TodoTasksSessionFactory's
    // remarks for why it exists (keeping the shared-cache in-memory database alive) and why sessions
    // deliberately do NOT open against it directly.
    containerBuilder.RegisterInstance(anchorConnection).SingleInstance();
    containerBuilder.RegisterInstance(sessionFactory).As<ISessionFactory>().SingleInstance();

    // Scoped so the whole dispatch - repository reads, IUnitOfWork writes - shares one session. Each
    // session opens its own independent connection the normal NHibernate way (OpenSession(), no forced
    // connection) - SQLite's shared-cache connection string (see TodoTasksSessionFactory) is what makes
    // every one of these independent connections see the same in-memory database.
    containerBuilder.Register(c => c.Resolve<ISessionFactory>().OpenSession()).As<ISession>().InstancePerLifetimeScope();

    containerBuilder.AddInterlocutorUnitOfWork(c => new NHibernateUnitOfWork(c.Resolve<Lazy<ISession>>()));

    // Each feature registers its own IDomainRepository<TEntity, TKey> here as it lands - see
    // documentation/beginners-guide-todo-api.md Step 4 for the shape every one of these follows. Every
    // repository below is built over the same scoped Lazy<ISession> as the unit of work above, which is
    // the actual mechanism behind "one unit of work, many repositories" (see Features/Users/DeleteUser,
    // an upcoming ticket, for where that's put to real use).
    containerBuilder.AddInterlocutorRepository<User, int, NHibernateRepository<User, int>>(c => new NHibernateRepository<
        User,
        int
    >(c.Resolve<Lazy<ISession>>(), u => u.Id));
    containerBuilder.AddInterlocutorRepository<TodoTask, int, NHibernateRepository<TodoTask, int>>(
        c => new NHibernateRepository<TodoTask, int>(c.Resolve<Lazy<ISession>>(), t => t.Id)
    );

    // FluentValidation's own IValidator<T> implementations (CreateUserValidator, and TodoTasks'
    // equivalents once that feature lands) - not something interlocutor.ScanAssembly below covers.
    // interlocutor.Pipeline.UseRecommendedChain()'s AddValidationBehavior() only wires up
    // ValidationBehavior<,> itself (conditionally, when Interlocutor.FluentValidation is referenced);
    // it deliberately leaves *which* validators exist to ordinary FluentValidation registration,
    // exactly like this - without it, ValidationBehavior finds zero validators for every request and
    // silently skips validation entirely (no error, just nothing enforced), which is worth knowing
    // before you go looking for why a validator "isn't running."
    containerBuilder
        .RegisterAssemblyTypes(typeof(Program).Assembly)
        .AsClosedTypesOf(typeof(IValidator<>))
        .InstancePerLifetimeScope();

    // UnitOfWorkBehavior's actual commit/rollback decision reads IOutcomeClassifier specifically, not
    // IResponseOutcomeDescriber - both need this project's custom success types registered
    // independently, or the commit decision keeps using the unmodified, fail-closed default even
    // after OverrideOutcomeDescriber below. See TodoTasksOutcomeDescriber's own remarks - this exact
    // gap was found and fixed the hard way, not assumed.
    containerBuilder.RegisterInstance(TodoTasksOutcomeDescriber.Classifier).As<IOutcomeClassifier>().SingleInstance();

    // Process-lifetime, not per-request: the whole point is remembering counts across dispatches.
    containerBuilder.RegisterType<InMemoryCompletionCounter>().As<ICompletionCounter>().SingleInstance();

    // AuthorizationBehavior's IRequiresPolicy gate - see DeleteTodoTaskCommand's own remarks for how
    // this contrasts with UpdateTodoTaskCommand's fine-grained, entity-based check.
    containerBuilder.RegisterType<TodoTaskPolicyEvaluator>().As<IPolicyEvaluator>().InstancePerLifetimeScope();

    containerBuilder.AddInterlocutor(interlocutor =>
    {
        interlocutor.ScanAssembly(typeof(Program).Assembly);

        // Without this, every write handler's genuinely successful custom-DTO outcome (UserDto, and
        // later TodoTaskDto) would be described incorrectly in logs/audit entries - see
        // TodoTasksOutcomeDescriber's own remarks for why the library's default classifier can't
        // recognize a project-specific success type on its own.
        interlocutor.Pipeline.OverrideOutcomeDescriber<TodoTasksOutcomeDescriber>();

        // Logging -> Auditing -> Authorization -> Validation -> Transaction, in that order - see
        // documentation/beginners-guide-todo-api.md Step 2 and documentation/pipeline-and-behaviors.md
        // for the reasoning behind this specific order. AddAuditingBehavior (part of this chain)
        // try-adds Interlocutor.Abstractions.Pipeline.LoggingAuditSink as the IAuditSink default, so
        // this project doesn't need - and deliberately doesn't write - its own copy of it.
        interlocutor.Pipeline.UseRecommendedChain();
    });
});

var app = builder.Build();

// Must run before any endpoint that calls RequireAuthorization() - this is what actually invokes
// DevHeaderAuthenticationHandler and populates HttpContext.User for the rest of the pipeline.
app.UseAuthentication();
app.UseAuthorization();

// Routes every [ApiController]-attributed controller's actions - TodoTasksController and UsersController,
// the MVC-controller twin of the minimal-API endpoints mapped below, under /api/todo-tasks and /api/users.
app.MapControllers();

// A trivially cheap liveness check - also doubles as proof, for TodoTasks.Api.Tests' HTTP fixture,
// that the whole host (Autofac container, NHibernate schema export, pipeline registration) came up
// cleanly before any feature endpoint exists to exercise instead.
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

app.Run();

/// <summary>
/// Exposes this file's top-level-statements entry point as a named type so
/// <c>WebApplicationFactory&lt;Program&gt;</c> (used throughout <c>TodoTasks.Api.Tests</c>' HTTP
/// integration tests) can host this application in-process.
/// </summary>
public partial class Program;
