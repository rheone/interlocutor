using System.Data.SQLite;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Interlocutor.Abstractions.Core;
using Interlocutor.AspNetCore;
using Interlocutor.DependencyInjection.Autofac;
using Interlocutor.Persistence.NHibernate;
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

// Built once, before the container exists, exactly like documentation/beginners-guide-todo-api.md
// Step 4/10 build their own session factory above ConfigureContainer - see TodoTasksSessionFactory's
// own remarks for why this project's version keeps one connection open rather than a connection string
// NHibernate would open and close per session.
var (sqliteConnection, sessionFactory) = TodoTasksSessionFactory.Build();

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

    // The kept-alive connection and the session factory built against it - see TodoTasksSessionFactory.
    containerBuilder.RegisterInstance(sqliteConnection).SingleInstance();
    containerBuilder.RegisterInstance(sessionFactory).As<ISessionFactory>().SingleInstance();

    // Scoped so the whole dispatch - repository reads, IUnitOfWork writes - shares one session. Every
    // session opens explicitly against the one kept-alive connection above (WithOptions().Connection(...))
    // rather than letting NHibernate open and close its own physical connection per session, which would
    // lose the in-memory database's contents the moment that connection closed.
    containerBuilder
        .Register(c => c.Resolve<ISessionFactory>().WithOptions().Connection(c.Resolve<SQLiteConnection>()).OpenSession())
        .As<ISession>()
        .InstancePerLifetimeScope();

    containerBuilder.AddInterlocutorUnitOfWork(c => new NHibernateUnitOfWork(c.Resolve<Lazy<ISession>>()));

    // Each feature registers its own IDomainRepository<TEntity, TKey> here as it lands - see
    // Features/Users/CreateUser and Features/TodoTasks/CreateTodoTask for the AddInterlocutorRepository
    // calls this project actually uses, and documentation/beginners-guide-todo-api.md Step 4 for the
    // shape they follow.

    containerBuilder.AddInterlocutor(interlocutor =>
    {
        interlocutor.ScanAssembly(typeof(Program).Assembly);

        // Logging -> Auditing -> Authorization -> Validation -> Transaction, in that order - see
        // documentation/beginners-guide-todo-api.md Step 2 and documentation/pipeline-and-behaviors.md
        // for the reasoning behind this specific order. AddAuditingBehavior (part of this chain)
        // try-adds Interlocutor.Abstractions.Pipeline.LoggingAuditSink as the IAuditSink default, so
        // this project doesn't need - and deliberately doesn't write - its own copy of it.
        interlocutor.Pipeline.UseRecommendedChain();
    });
});

var app = builder.Build();

// A trivially cheap liveness check - also doubles as proof, for TodoTasks.Api.Tests' HTTP fixture,
// that the whole host (Autofac container, NHibernate schema export, pipeline registration) came up
// cleanly before any feature endpoint exists to exercise instead.
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

// Feature endpoints are mapped here as each one lands - see each Features/**/Map*Endpoint(app) extension
// method, e.g. Features/Users/CreateUser/CreateUserEndpoint.cs.

app.Run();

/// <summary>
/// Exposes this file's top-level-statements entry point as a named type so
/// <c>WebApplicationFactory&lt;Program&gt;</c> (used throughout <c>TodoTasks.Api.Tests</c>' HTTP
/// integration tests) can host this application in-process.
/// </summary>
public partial class Program;
