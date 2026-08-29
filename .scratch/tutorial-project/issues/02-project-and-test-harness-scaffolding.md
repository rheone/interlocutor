Status: open

## Labels

ready-for-agent

## What to build

The empty, runnable skeleton every later ticket builds on. No business features yet - this is the prefactor that makes every vertical slice easy to add.

**Source project** - `TutorialProject/TodoTasks.Api/TodoTasks.Api.csproj`, minimal API (`dotnet new web` shape), matching `documentation/beginners-guide-todo-api.md` Steps 1-2 and 4:

- `TargetFramework net11.0`, `EnablePreviewFeatures`, `Nullable enable`, `ImplicitUsings enable` (this project does not inherit the repo's `Directory.Build.props` package-authoring properties like `IsPackable` in a meaningful way since it never ships - set `IsPackable=false`, matching how the old `samples/` projects were marked).
- Package references: `Interlocutor.Abstractions`, `Interlocutor.DependencyInjection.Autofac`, `Interlocutor.Persistence.NHibernate`, `Interlocutor.AspNetCore`, `Interlocutor.FluentValidation` (all as project references to `src/*`, not NuGet, since this lives in-repo), plus `Autofac.Extensions.DependencyInjection`, `NHibernate`, `FluentNHibernate`, and `Microsoft.Data.Sqlite` (new - add to `Directory.Packages.props`, pin to the same version already used by `Microsoft.EntityFrameworkCore.Sqlite` there if compatible, otherwise the latest stable compatible with NHibernate 5.7.0's ADO driver).
- `Program.cs`: `WebApplication.CreateBuilder`, `UseServiceProviderFactory(new AutofacServiceProviderFactory())`, `AddHttpContextAccessor()`, `AspNetCoreRequestContextAccessor` registered before `AddInterlocutor`, `AddInterlocutor(interlocutor => { interlocutor.ScanAssembly(typeof(Program).Assembly); interlocutor.Pipeline.UseRecommendedChain(); })` - exactly the guide's Step 2 shape, empty `Features/` folder ready for later tickets.
- NHibernate session factory: `Fluently.Configure().Database(SQLiteConfiguration.Standard...)` targeting an in-memory SQLite database over **one ADO.NET connection kept open for the app's whole lifetime** (open it explicitly at startup, register it as a singleton, build the `ISessionFactory`'s connection off it rather than a connection string NHibernate opens/closes itself) - per the PRD's persistence decision, this is what keeps the schema alive across requests. Export the schema against that same connection at startup (`SchemaExport().Execute(...)` or equivalent) since there are no migrations for a POC. `ISession` registered `InstancePerLifetimeScope` via `Lazy<ISession>`, exactly like the guide's Step 4.
- `IAuditSink` → `LoggingAuditSink` (guide's Step 5 shape) registered here since every write handler benefits from it immediately.
- No feature endpoints yet - `app.Run()` with nothing mapped is fine for this ticket; a smoke test (below) proves the app boots and the schema exports cleanly rather than proving any endpoint works.

**Test harness** - `tests/TodoTasks.Api.Tests/TodoTasks.Api.Tests.csproj` (or split into `TodoTasks.Api.UnitTests`/`TodoTasks.Api.PersistenceTests`/`TodoTasks.Api.HttpTests` sub-projects if that reads more cleanly once you're in the code - either is fine, pick one and note the choice in this ticket's `## Comments`), referencing `TodoTasks.Api.csproj` and xunit.v3/NSubstitute/`Microsoft.Testing.Platform`/coverlet exactly like this repo's other `tests/*.Tests` projects. Set up, but don't yet use for a real feature:

- A base fixture for **unit tests**: nothing framework-specific needed beyond NSubstitute, but document the pattern (mock `IDomainRepository<TEntity,TKey>`/`IUnitOfWork` per handler test) in a short XML doc comment or README note in the test project.
- A fixture for **persistence/mapping integration tests**: builds a real `ISessionFactory` against a real in-memory SQLite connection (mirroring the app's own single-keep-alive-connection setup), with schema export, so a later ticket's test can `session.Save`/`session.Get` a mapped entity and assert a real round-trip.
- A fixture for **HTTP integration tests**: `WebApplicationFactory<Program>` (requires `Program` to be `public partial class Program` or otherwise accessible - add that if the minimal API top-level-statements shape hides it) wired against the same in-memory SQLite setup, so later tickets can issue real HTTP requests including an `X-User-Id` header once ticket 04 exists.

**Solution wiring**: add `TutorialProject/TodoTasks.Api/TodoTasks.Api.csproj` and the test project(s) to `Interlocutor.slnx` under a new `/TutorialProject/` folder.

## Acceptance criteria

- [ ] `dotnet run --project TutorialProject/TodoTasks.Api` starts the app without error and the NHibernate schema export against in-memory SQLite succeeds (verify via a passing smoke test, not just manual observation)
- [ ] The single keep-alive connection is documented with an XML/inline comment explaining *why* it's kept open (in-memory SQLite loses its schema when its one connection closes) and pointing at where it's opened
- [ ] `Microsoft.Data.Sqlite` is centrally pinned in `Directory.Packages.props`, not version-pinned per-project
- [ ] All three test-harness fixtures exist and each has at least one passing smoke test proving it works (session factory opens and can create/drop schema; `WebApplicationFactory` boots and returns a response from *some* endpoint - a trivial `app.MapGet("/", ...)` placeholder is fine to prove the factory works, remove it once ticket 05+ add real endpoints if it stops being useful)
- [ ] `TutorialProject/TodoTasks.Api` and its test project(s) are in `Interlocutor.slnx`
- [ ] `dotnet build` and `dotnet test` succeed for the whole solution
- [ ] `dotnet csharpier .` produces no diff
- [ ] Every public member has an XML doc comment; inline comments explain the non-obvious infrastructure choices (single connection, Autofac-before-AddInterlocutor ordering) the way the guide's own prose does, in code-comment form

## Blocked by

01
