# Tutorial Project: TodoTasks.Api

Status: planning complete (grilling + to-tickets), implementation starting. Tracked as issues under `.scratch/tutorial-project/issues/`.

This replaces `samples/` with a single, fully-implemented, runnable project that *is* [documentation/beginners-guide-todo-api.md](../../documentation/beginners-guide-todo-api.md) - every "blank" filled in, TDD'd, with the guide's own Bonus sections held back as a deliberate backlog. It is a proof-of-concept only; it will never be released. This file is the living record of scope and decisions - update it as the effort evolves, don't treat it as a one-shot ticket to close and forget.

**Implementation happens in its own git worktree/branch** (`worktree-tutorial-project-todo-api`, off `main`), never on a docs branch.

## Original request (verbatim)

> There exists a Beginner's Guide at documentation/beginners-guide-todo-api.md
>
> There is also a sample set at samples/\* that is out of date. Assuming there is nothing special in the two samples, lets get rid of them and instead fully implement a new csproj laid out in Vertical Stack (but by directory rather than projects) that implements the beginners guide application.
> It should be full implementation with the "blanks" filled in
>
> come up with an implementation plan and use TDD with good coverage
>
> It would seem appropriate to use an in memory SQL database
>
> This project will never be released, it acts as a working proof of concepts.
>
> Make sure the code has rich value added comments and xml documents (for all members) to provide clarity
>
> Leave the project open for extension, we'll want to add some advanced features to it (to provide examples)
>
> This should be minimal API project using NHibernate, Fluent Validation and Autofac (same tech as what is used in the beginners guide)
>
> Code should be clean and easy enough to follow for a junior developer
>
> Code comments should also make clear where the implementation of something can be found if relevant
>
> This will be a living document

Mid-turn addendum: **implementation should be done on its own worktree.**

## Decisions from the grilling session

- **Scope**: the guide's core running example only (Steps 1-10, the 8-request table: Create, GetById, List, Update, Complete, Reopen, Delete, ClearCompleted), plus one feature not in the guide at all - **User management** (see below). The guide's own **Bonus sections become a ticketed backlog**, not built now: manual response mapping / MVC controller variant, QueryOver title search, paging, `Cancel`-without-failure rollback, a second custom pipeline behavior, reordering Validation before Authorization. Nothing invented beyond what the guide's Bonus sections already cover.
- **Location**: new top-level solution folder `TutorialProject/`, replacing `/samples/`. One project: `TutorialProject/TodoTasks.Api/TodoTasks.Api.csproj`. Tests: `tests/TodoTasks.Api.Tests/` (or split unit/persistence/http sub-projects - decided during scaffolding, ticket 02). Both added to `Interlocutor.slnx`, buildable/testable from the repo root with plain `dotnet build`/`dotnet test`.
- **Structure**: vertical slices **by directory, one folder per use case** (not one folder per entity) - e.g. `Features/TodoTasks/CreateTodoTask/`, `Features/TodoTasks/CompleteTodoTask/`, `Features/Users/CreateUser/`, `Features/Users/DeleteUser/`. Each folder colocates its Command/Query, validator (if any), handler, and endpoint mapping (`MapXEndpoint(app)`, called from `Program.cs`).
- **Stack**: NHibernate + Fluent NHibernate mapping, FluentValidation, Autofac, `interlocutor.Pipeline.UseRecommendedChain()` - exactly the guide's stack, no swaps.
- **User feature**: `User` (Id, DisplayName, Email), create/get/delete. `TodoTask.OwnerId` stays an opaque string - no DB-level foreign key, matching the guide's own modeling exactly. `CreateUserCommand` gets a duplicate-email `Conflict` check, mirroring `CreateTodoTaskCommand`'s duplicate-title `Conflict` (same arity pattern, second worked example).
- **Multi-repository unit-of-work demo**: `DeleteUserCommand` loads the user, deletes every `TodoTask` they own via `IDomainRepository<TodoTask,int>`, deletes the user via `IDomainRepository<User,...>`, one `IUnitOfWork.SaveChangesAsync()` commits all of it. This is the guide's own "multiple repositories, one unit of work" Bonus pattern, made real instead of staying an illustrative activity-log aside.
- **Caller identity**: a minimal dev/test `AuthenticationHandler` reads `X-User-Id` and validates it against the **User repository** - no matching user, no auth (401). Ties the two features together: create a user, then act as them. `AspNetCoreRequestContextAccessor` (from `Interlocutor.AspNetCore`) reads `HttpContext.User` exactly as the guide describes; nothing custom on the Interlocutor side.
- **Persistence**: in-memory SQLite via `System.Data.SQLite.Core` and NHibernate's own `SQLite20Driver` + `SQLiteDialect` (FluentNHibernate's `SQLiteConfiguration.Standard` default) - **not** `Microsoft.Data.Sqlite` as originally assumed during grilling; verified by prototype that NHibernate 5.7.0 (the version this repo pins) ships no `Microsoft.Data.Sqlite`-based driver at all, only `SQLite20Driver` (System.Data.SQLite) and `CsharpSqliteDriver`. **Revised during ticket 03** (see that ticket's own Comments for the full account): the originally-planned "one kept-alive connection object, force every session to share it via `WithOptions().Connection(...)`" approach compiles and passes a simple smoke test but silently loses data across real, independently-scoped sessions - NHibernate's own session lifecycle resets it somewhere in there, verified empirically, not assumed. Replaced with SQLite's **shared-cache mode**: one anchor connection kept alive per app/test-fixture instance (using a connection string uniquely named per `TodoTasksSessionFactory.Build()` call - a bare, unnamed shared-cache URI is process-wide and would let every caller collide on one database), with every `ISession` opening its own independent connection the normal NHibernate way. See `TutorialProject/TodoTasks.Api/Persistence/TodoTasksSessionFactory.cs`'s own remarks for the mechanism.
- **Outcome classification**: every custom success DTO used as an `OperationOutcome<TSuccess,...>`'s `TSuccess` (`UserDto`, `TodoTaskDto`, ...) must be registered with a project-wide `IOutcomeClassifier` (`Infrastructure/TodoTasksOutcomeDescriber.cs`), or `UnitOfWorkBehavior`'s default classifier fail-closes it as a failure and silently rolls back every genuinely successful write. Found and fixed during ticket 03 (see that ticket's Comments) - requires **two** separate DI registrations in `Program.cs` (`OverrideOutcomeDescriber<TodoTasksOutcomeDescriber>()` for logging/description, plus a direct `IOutcomeClassifier` registration for the actual commit/rollback decision - the first alone does not fix the second). Every future ticket that introduces a new success DTO must add it to `TodoTasksOutcomeDescriber.Classifier`.
- **Feature-area folder naming**: `Features/Tasks/`, not `Features/TodoTasks/` - the latter collides with the project's own root namespace `TodoTasks.Api` (C#'s `using`-resolution inside a namespace block walks outward and commits to the first matching segment it finds, breaking `using TodoTasks.Api.*` in *every* file under `Features/**`, not just the colliding subfolder - found during ticket 05, see that ticket's Comments for the full mechanism).
- **`DateTimeOffset` columns need a custom `IUserType`**: SQLite's NHibernate dialect has no mapping for `DbType.DateTimeOffset` at all (confirmed by the actual exception, not assumed - SQLite has no native timestamp-with-offset type). `Persistence/DateTimeOffsetAsStringUserType.cs` (found during ticket 05) stores the value as a round-trippable ISO 8601 string instead - reuse it for any future `DateTimeOffset` column.
- **Tests**: all three layers - unit tests per handler/validator (NSubstitute-mocked `IDomainRepository`/`IUnitOfWork`, mirrors this repo's own `tests/*.Tests` convention), NHibernate mapping/persistence integration tests against real in-memory SQLite, and full HTTP integration tests via `WebApplicationFactory<Program>` exercising the dev-auth flow end to end.
- **Docs**: `documentation/sample-app-walkthrough.md` and `CLAUDE.md`'s `samples/` references get rewritten in the same effort (not deferred) - required by this repo's own `maintainer_docs/agents/documentation-maintenance.md` policy, since both currently describe the project being deleted. The beginner's guide's own line ("unlike samples/, there's no runnable project behind it") also needs correcting, since after this effort a runnable project implementing exactly that guide will exist.
- **Out of scope / not applicable**: `tests/Interlocutor.Architecture.Tests/DependencyTests.cs` - `TutorialProject/TodoTasks.Api` is not a `src/*` package and isn't subject to the package-layering rules that test enforces.

## Ticket checklist

Active (`ready-for-agent`):

- [x] **01 - Remove the outdated samples/ projects**
- [x] **02 - Project & test-harness scaffolding**
- [x] **03 - Users feature: Create & Get**
- [x] **04 - Dev/test authentication handler**
- [x] **05 - TodoTasks feature: Create**
- [ ] **06 - TodoTasks feature: Read (GetById + List)**
- [ ] **07 - TodoTasks feature: Update**
- [ ] **08 - TodoTasks feature: Complete/Reopen + notification**
- [ ] **09 - TodoTasks feature: Delete + policy authorization**
- [ ] **10 - TodoTasks feature: ClearCompleted**
- [ ] **11 - Users feature: Delete (multi-repo cascade)**
- [ ] **12 - Documentation sync**

Backlog (`needs-triage`, deferred - do not start until explicitly promoted to `ready-for-agent`), one per guide Bonus section:

- [ ] **13 - Manual response mapping + MVC controller variant**
- [ ] **14 - QueryOver title search**
- [ ] **15 - Paging**
- [ ] **16 - Cancel-without-failure rollback**
- [ ] **17 - Second custom pipeline behavior (slow-handler warning)**
- [ ] **18 - Reordered pipeline (Validation before Authorization)**

## Process notes for whoever resumes this effort

- Tickets 01-05 are a strict linear chain (each blocks the next). 06/07/08/09/11 all unblock in parallel once 05 lands. 10 waits on 08. 12 waits on everything (06-11).
- Prefer implementing the linear chain (01-05) as one continuous session so context about the scaffolding decisions carries forward; the parallel fan-out (06/07/08/09/11) is a good place to split into separate agent runs if desired, since each touches a disjoint TodoTasks/Users use case.
- After each ticket: run `dotnet build`, run that ticket's tests, run `dotnet csharpier .`, commit with a `feat:`/`test:` Conventional Commit message, check the ticket's box in this file and in its own issue file, append a short `## Comments` note to the issue file recording what actually happened (matches this repo's existing ticket convention).
- Run the *full* `dotnet test` suite (not just the new project) before considering the effort done, to confirm nothing in `src/`/`tests/` regressed from the `Directory.Packages.props` addition (`Microsoft.Data.Sqlite`) or the `Interlocutor.slnx` edits.
