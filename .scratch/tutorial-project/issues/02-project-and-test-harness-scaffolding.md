Status: done

## Labels

ready-for-agent

## What to build

The empty, runnable skeleton every later ticket builds on. No business features yet - this is the prefactor that makes every vertical slice easy to add.

**Source project** - `TutorialProject/TodoTasks.Api/TodoTasks.Api.csproj`, minimal API (`dotnet new web` shape), matching `documentation/beginners-guide-todo-api.md` Steps 1-2 and 4:

- `TargetFramework net11.0`, `EnablePreviewFeatures`, `Nullable enable`, `ImplicitUsings enable` (this project does not inherit the repo's `Directory.Build.props` package-authoring properties like `IsPackable` in a meaningful way since it never ships - set `IsPackable=false`, matching how the old `samples/` projects were marked).
- Package references: `Interlocutor.Abstractions`, `Interlocutor.DependencyInjection.Autofac`, `Interlocutor.Persistence.NHibernate`, `Interlocutor.AspNetCore`, `Interlocutor.FluentValidation` (all as project references to `src/*`, not NuGet, since this lives in-repo), plus `Autofac.Extensions.DependencyInjection`, `NHibernate`, `FluentNHibernate`, and **`System.Data.SQLite.Core`** (new - add to `Directory.Packages.props`, pinned; **not** `Microsoft.Data.Sqlite` - verified by prototype that NHibernate 5.7.0 has no driver for it, only `SQLite20Driver`, which wraps `System.Data.SQLite`).
- `Program.cs`: `WebApplication.CreateBuilder`, `UseServiceProviderFactory(new AutofacServiceProviderFactory())`, `AddHttpContextAccessor()`, `AspNetCoreRequestContextAccessor` registered before `AddInterlocutor`, `AddInterlocutor(interlocutor => { interlocutor.ScanAssembly(typeof(Program).Assembly); interlocutor.Pipeline.UseRecommendedChain(); })` - exactly the guide's Step 2 shape, empty `Features/` folder ready for later tickets. Make the top-level-statements `Program` class reachable for `WebApplicationFactory<Program>` (a `public partial class Program;` declaration alongside the top-level statements is the usual fix).
- NHibernate session factory: open one `System.Data.SQLite.SQLiteConnection("Data Source=:memory:;Version=3;New=True;")` **explicitly at startup and keep it open for the app's whole lifetime** (register the connection itself as an Autofac singleton) - per the PRD's persistence decision, this is what keeps the schema alive across requests, since an in-memory SQLite database ceases to exist the instant its one connection closes. Build the `ISessionFactory` via `Fluently.Configure().Database(SQLiteConfiguration.Standard.ConnectionString(...)).Mappings(...).BuildSessionFactory()` (the connection string here only satisfies NHibernate's config validation - it is never actually used to open a connection, because every session below is opened explicitly against the kept-alive connection instead). Export the schema against that same kept-alive connection at startup via `new SchemaExport(configuration).Execute(useStdOut: false, execute: true, justDrop: false, connection: keepAliveConnection, exportOutput: null)`, since there are no migrations for a POC. Register `Lazy<ISession>` scoped (`InstancePerLifetimeScope`), resolved as `sessionFactory.WithOptions().Connection(keepAliveConnection).OpenSession()` - **not** the obsolete `OpenSession(DbConnection)` overload.
- ~~`IAuditSink` → `LoggingAuditSink` registered here~~ - turned out unnecessary: `Interlocutor.Abstractions.Pipeline.LoggingAuditSink` already ships as `AddAuditingBehavior`'s (part of `UseRecommendedChain()`) try-added default, doing exactly this job. See `## Comments`.
- No feature endpoints yet - `app.Run()` with nothing mapped is fine for this ticket; a smoke test (below) proves the app boots and the schema exports cleanly rather than proving any endpoint works.

**Test harness** - `tests/TodoTasks.Api.Tests/TodoTasks.Api.Tests.csproj` (or split into `TodoTasks.Api.UnitTests`/`TodoTasks.Api.PersistenceTests`/`TodoTasks.Api.HttpTests` sub-projects if that reads more cleanly once you're in the code - either is fine, pick one and note the choice in this ticket's `## Comments`), referencing `TodoTasks.Api.csproj` and xunit.v3/NSubstitute/`Microsoft.Testing.Platform`/coverlet exactly like this repo's other `tests/*.Tests` projects. Set up, but don't yet use for a real feature:

- A base fixture for **unit tests**: nothing framework-specific needed beyond NSubstitute, but document the pattern (mock `IDomainRepository<TEntity,TKey>`/`IUnitOfWork` per handler test) in a short XML doc comment or README note in the test project.
- A fixture for **persistence/mapping integration tests**: builds a real `ISessionFactory` against a real in-memory SQLite connection (mirroring the app's own single-keep-alive-connection setup), with schema export, so a later ticket's test can `session.Save`/`session.Get` a mapped entity and assert a real round-trip.
- A fixture for **HTTP integration tests**: `WebApplicationFactory<Program>` (requires `Program` to be `public partial class Program` or otherwise accessible - add that if the minimal API top-level-statements shape hides it) wired against the same in-memory SQLite setup, so later tickets can issue real HTTP requests including an `X-User-Id` header once ticket 04 exists.

**Solution wiring**: add `TutorialProject/TodoTasks.Api/TodoTasks.Api.csproj` and the test project(s) to `Interlocutor.slnx` under a new `/TutorialProject/` folder.

## Acceptance criteria

- [x] `dotnet run --project TutorialProject/TodoTasks.Api` starts the app without error and the NHibernate schema export against in-memory SQLite succeeds (verify via a passing smoke test, not just manual observation)
- [x] The single keep-alive connection is documented with an XML/inline comment explaining *why* it's kept open (in-memory SQLite loses its schema when its one connection closes) and pointing at where it's opened
- [x] `System.Data.SQLite.Core` is centrally pinned in `Directory.Packages.props`, not version-pinned per-project (not `Microsoft.Data.Sqlite` - see `## Comments`)
- [x] All three test-harness fixtures exist and each has at least one passing smoke test proving it works - `PersistenceFixture`/`PersistenceFixtureTests`, `TodoTasksApiFactory`/`TodoTasksApiFactoryTests` (hits a permanent `/health` endpoint rather than a throwaway placeholder)
- [x] `TutorialProject/TodoTasks.Api` and its test project(s) are in `Interlocutor.slnx`
- [x] `dotnet build` and `dotnet test` succeed for the whole solution (823/823 passing, up from 820/820 after ticket 01)
- [x] `dotnet csharpier .` produces no diff
- [x] Every public member has an XML doc comment; inline comments explain the non-obvious infrastructure choices (single connection, Autofac-before-AddInterlocutor ordering) the way the guide's own prose does, in code-comment form

## Blocked by

01

## Comments

Three corrections to the ticket's own assumptions, found during implementation (all verified empirically, not assumed - see `.scratch/tutorial-project/PRD.md`'s persistence-decision note):

1. **SQLite driver**: `System.Data.SQLite.Core`, not `Microsoft.Data.Sqlite` - NHibernate 5.7.0 ships no driver for the latter at all (confirmed against the shipped assembly). Prototyped the full "one kept-alive connection, two different `ISession`s see the same data" pattern standalone before wiring it into the real project - see the prototype under the session's scratchpad if it's still present, otherwise `Persistence/TodoTasksSessionFactory.cs`'s own remarks capture the same reasoning. Used the non-obsolete `ISessionFactory.WithOptions().Connection(...).OpenSession()`, not the obsolete `OpenSession(DbConnection)` overload the ticket's own draft code snippet implied.
2. **No custom `LoggingAuditSink`**: `Interlocutor.Abstractions.Pipeline.LoggingAuditSink` is already `AddAuditingBehavior`'s try-added default (confirmed by reading `InterlocutorPipelineBuilder.AddAuditingBehavior` and the startup registration report) - writing a project-local copy would just be a same-named duplicate (and, since both would be `using`-imported in `Program.cs`, an actual `CS0104` ambiguous-reference compile error). Nothing registered for `IAuditSink`; the library's default carries the whole project.
3. **`WebApplicationFactory<Program>` vs. the installed SDK**: `Microsoft.AspNetCore.Mvc.Testing` pinned to the same `preview.7` as this repo's other `Microsoft.Extensions.*` packages threw `MissingMethodException` (a `Microsoft.Extensions.Primitives.ChangeToken.OnChange` overload preview.7's `Microsoft.Extensions.Configuration.Json` expects, that the actually-installed `preview.6` shared framework doesn't have) the moment `Program.cs`'s real `WebApplication.CreateBuilder` loaded `appsettings.json` through the file-watching config provider - a codepath none of this repo's other `preview.7`-pinned test packages happen to exercise. Pinned `Microsoft.AspNetCore.Mvc.Testing` alone to `preview.6.26359.118` (matching the installed SDK/shared framework per `global.json`) rather than touching the other packages' pins, which were unaffected and still pass. Diagnosed by reproducing directly rather than guessing - `dotnet --info` confirmed the installed/referenced version skew.

One test project (not three), organized into `Unit/`/`Persistence/`/`Http/` subfolders with a `README.md` explaining the convention - simpler solution wiring than three separate `.csproj` files, still gives each layer its own clearly-scoped home.
