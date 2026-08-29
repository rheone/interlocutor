# ADR-0017: Optional Persistence Abstraction - Domain Repository and Unit of Work

**Status:** Accepted

## Context

Handlers that need storage today inject `Lazy<TContext>` (EF Core) or `Lazy<ISession>` (NHibernate) directly, per `documentation/transactions-and-store-integration.md`. This works and is the currently-shipped, coherent design - but a consumer who wants a narrower, store-agnostic CRUD/paging surface (for testability, or to avoid hand-rolling the same `GetById`/paging query shape in every handler) has no library-provided option.

A proposal was raised to add optional `IDomainRepository`/`IUnitOfWork`/`IDomainService` types, mirroring the existing `Interlocutor.DependencyInjection.{Abstractions,Autofac,Microsoft}` neutral-package-plus-implementations split, for both EF Core and NHibernate. Prior research (`.scratch/unit-of-work-research.md`) surveyed the existing package layering, the `ITransactionalRequest`/`TransactionBehavior` model, and external prior art (the legacy EF5/MVC4 repository+UoW tutorial, Microsoft's current EF Core infrastructure-persistence-layer guidance, NHibernate's `ISession` transaction docs, and ABP Framework's `IRepository`/`IUnitOfWork`).

Two facts already on record constrain this decision:

- `documentation/glossary.md`'s "Transaction scope" entry: one dispatch = one DI scope = one scoped `DbContext`/`ISession`, shared by `TransactionBehavior` and every handler in that dispatch. The mediator never opens its own scope.
- `CLAUDE.md`: `PagedResult<T>` is explicitly not a shipped `Interlocutor.CommonKernel` type - pagination is deliberately left to consumers to define as their own success-case type.

## Decision

Add a new, optional, opt-in persistence-abstraction layer. Nothing about this ADR changes `TransactionBehavior`'s existing public contract, `ITransactionalRequest`/`IExternalStoreRequest`'s semantics, or the direct-`DbContext`/`ISession`-injection path documented today - that path remains fully supported and is not being replaced.

### Package layout

Three new packages, mirroring the DI package split's neutral-plus-implementations shape:

| Package | Contents | References |
|---|---|---|
| `Interlocutor.Persistence.Abstractions` | `IUnitOfWork`, `IDomainRepository<TEntity, TKey>`, paging/sort types | `Interlocutor.Abstractions` (project reference) only, zero ORM dependency |
| `Interlocutor.Persistence.EntityFrameworkCore` | `DbContext`-backed implementation | `Interlocutor.Persistence.Abstractions` + `Microsoft.EntityFrameworkCore` |
| `Interlocutor.Persistence.NHibernate` | `ISession`-backed implementation | `Interlocutor.Persistence.Abstractions` + `NHibernate` |

New packages, not folded into the existing `Interlocutor.EntityFrameworkCore`/`Interlocutor.NHibernate` packages: this keeps the persistence abstraction separately adoptable from the transaction-behavior packages every EF/NHibernate consumer already takes, preserving "optional" at the package level, not just the type level.

`tests/Interlocutor.Architecture.Tests/DependencyTests.cs` gains rules mirroring the existing DI-split assertions: `Interlocutor.Persistence.Abstractions` must not depend on AspNetCore, either store package (existing or new), `FluentValidation`, or either `DependencyInjection.*` package; `Interlocutor.Persistence.EntityFrameworkCore`/`.NHibernate` must each depend on `Interlocutor.Persistence.Abstractions` and must not depend on each other.

Trim-compatible (`IsTrimmable=true`), not AOT-compatible, matching every other package in the solution (ADR-0016) - not attempted as a separate effort for this feature.

### `IUnitOfWork` is a façade, not a second transaction owner

`IUnitOfWork` wraps the *same* scoped `DbContext`/`ISession` that `TransactionBehavior` already manages. It does not open, commit, or roll back a transaction itself - transaction begin/commit/rollback stays exactly where it is today, inside `TransactionBehavior`, driven by `ITransactionalRequest` and the outcome classifier. This satisfies "operations within a Unit of Work must run in the same transaction and session" using the mechanism that already guarantees it (scoped mediator, one scoped session per dispatch), rather than introducing a second, parallel transaction-ownership model that `TransactionBehavior`'s documented contract and existing consumers would then need to reconcile against.

Scope lifetime is session-per-dispatch only, matching every other scoped dependency in the pipeline - no support for a longer-lived (e.g. session-per-conversation) UoW scope. `IUnitOfWork` and `IDomainRepository<T>` implementations are registered scoped and resolved via `Lazy<T>`, the same idiom `TransactionBehavior` already uses so a dispatch that touches neither never pays for session construction. Neither type implements `IAsyncDisposable` itself; disposal of the underlying `DbContext`/`ISession` remains the DI container's job, exactly as it is today.

### `IDomainRepository<TEntity, TKey>`

Generic and unconstrained - no `IAggregateRoot` marker interface. Interlocutor has no aggregate-root concept anywhere in the codebase today, and introducing one is a materially bigger design step than this feature needs to force. Noted for a likely future addition: a stricter `IAggregateRoot`-constrained repository variant, added later as a non-breaking opt-in layer alongside the unconstrained one, following the pattern Microsoft's own EF Core guidance recommends (one repository per aggregate root, not per table).

Members:

- `GetById(TKey id)` ships as a C# default interface method, built on the `IQueryable<TEntity>` escape hatch (`Queryable.FirstOrDefault(e => e.Id == id)` in terms of the member below). Every other member is abstract and implemented per ORM.
- Standard CRUD: insert, update, delete.
- Get-many and get-paged, taking an `Expression<Func<TEntity,bool>>` filter and an ordered list of `(Expression<Func<TEntity,object>>, SortDirection)` sort criteria (multi-column sort), plus page number and page size (not skip/take - matches "current page, page count" from the originating request, and is the more natural vocabulary for a UI-facing paged list). Returns a paging result type owned by `Interlocutor.Persistence.Abstractions`, distinct from `Interlocutor.CommonKernel` - `CommonKernel`'s absence of a shipped `PagedResult<T>` is unchanged by this decision; the new package's paging vocabulary is a different concern, for a different consumer (repository return shape, not result-outcome success-case shape).
- An `IQueryable<TEntity>` escape hatch for ad hoc composition beyond the fixed method set. Both EF Core's `DbContext` and NHibernate's `ISession` expose LINQ providers, so one interface method covers both implementations without an ORM-specific variant. Both prior-art sources surveyed (the legacy tutorial, ABP) converged on needing an escape hatch of this shape; a closed surface just pushes consumers back to direct `DbContext`/`ISession` injection for anything unanticipated.
- Get-single returns a nullable/`Maybe`-shaped result; the repository package does not decide which `OperationOutcome` case a "not found" maps to. Per `CLAUDE.md`'s result-type convention, only the handler decides which `OperationOutcome<...>` arity/cases an operation produces - `Interlocutor.Persistence.Abstractions` does not take a dependency on outcome-case semantics beyond what `Interlocutor.Abstractions` already brings in.

Resolved via constructor injection per entity type (`IDomainRepository<Order, Guid>` injected directly), not through an `IUnitOfWork.Repository<T>()` factory method. This matches Interlocutor's existing constructor-injection idiom and keeps `IUnitOfWork`'s own surface minimal, consistent with its role as a façade rather than a service locator.

### `IDomainService`

Out of scope for this decision. No internal or external source surveyed gave it a concrete shape distinct from either a plain handler or `ResourceCommandHandler`'s existing load-authorize-handle template. Deferred to a future ADR once a concrete driving use case exists.

### Documentation follow-up

`CLAUDE.md`'s pagination sentence and `documentation/glossary.md`'s Pagination section both currently read, in the present tense, as "Interlocutor's core has no opinion about pagination." That remains true of `CommonKernel`; it needs a clarifying note once `Interlocutor.Persistence.Abstractions` ships its own paging types, so a future reader doesn't read the new package as contradicting the existing sentence.

## Consequences

- **Positive:** Consumers get an optional, testable, store-agnostic CRUD/paging surface without being forced onto it - the direct `Lazy<DbContext>`/`Lazy<ISession>` path stays fully supported and undeprecated.
- **Positive:** "Same transaction and session" is satisfied by construction, using the mechanism that already guarantees it, rather than a second competing transaction-ownership model.
- **Positive:** The package split keeps the persistence abstraction independently adoptable from `TransactionBehavior`, and keeps `EntityFrameworkCore`/`NHibernate` mutually independent, mirroring how `.DependencyInjection.Microsoft`/`.Autofac` never depend on each other.
- **Negative:** Three new packages (plus matching test projects) to build, document, and maintain, for a feature with no identified internal consumer yet - accepted because the brief explicitly asked for optional/opt-in surface area, not a required capability.
- **Negative:** An unconstrained `IDomainRepository<T>` permits a repository-per-table anti-pattern that Microsoft's own EF Core guidance warns against; accepted for now in exchange for not forcing an aggregate-root concept into the codebase before one is otherwise needed. Revisit if this proves to be a real footgun in practice.
- **Negative:** `CLAUDE.md` and `documentation/glossary.md` need a coordinated edit once this ships, so the pagination section doesn't read as stale/contradictory next to the new package.

## Alternatives considered

- **Bolt `IUnitOfWork`/`IDomainRepository<T>` directly onto `Interlocutor.Abstractions`:** rejected - `Interlocutor.Abstractions`'s whole point is mediator/handler/pipeline concepts; every AspNetCore-only or FluentValidation-only consumer would inherit persistence-shaped types they never use.
- **Fold EF Core/NHibernate implementations into the existing `Interlocutor.EntityFrameworkCore`/`Interlocutor.NHibernate` packages:** structurally sound, considered a real alternative in the research doc, but rejected in favor of new packages to keep the persistence abstraction adoptable independently of the transaction-behavior packages.
- **`IUnitOfWork` owns begin/commit/rollback directly (ABP's shape), replacing `TransactionBehavior` as transaction owner:** rejected - a materially bigger change requiring a compatibility review of `TransactionBehavior`'s current public contract, for no identified benefit over the façade option.
- **`IUnitOfWork.Repository<T>()` factory / service-locator shape:** rejected in favor of constructor injection per entity type, matching the library's existing injection idiom.
- **`IAggregateRoot`-constrained repository now:** rejected for this iteration - no aggregate-root concept exists anywhere in the codebase; deferred as a likely future non-breaking addition.
- **`PagedResult<T>` shipped in `CommonKernel`:** rejected - would contradict `CLAUDE.md`'s current, deliberate framing of pagination as out of `CommonKernel`'s opinion. The new paging type lives in `Interlocutor.Persistence.Abstractions` instead, a different package for a different concern.
- **Typed `FilterSpec`/`SortSpec` DSL:** considered, rejected in favor of `Expression<Func<TEntity,bool>>` filters and an ordered `(Expression, SortDirection)` list for sort - no prior art surveyed (legacy tutorial, ABP) standardizes a bespoke filter DSL; both fall back to caller-supplied lambdas or an `IQueryable` escape hatch.
