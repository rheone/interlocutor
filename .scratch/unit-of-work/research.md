# Research: Domain Service / Domain Repository / Unit of Work abstraction

Status: research only - no design decision, no implementation, no ADR. Written to inform a future
design conversation about whether/how to add an optional persistence-abstraction layer (Repository +
Unit of Work + paging) on top of the existing `ITransactionalRequest` / `TransactionBehavior` model,
mirrored EF Core and NHibernate, package-split like `Interlocutor.DependencyInjection.{Abstractions,Autofac,Microsoft}`.

Hard requirement carried through this doc: **operations inside one Unit of Work must run in the same
transaction and the same session**, and every new type must be scoped correctly under DI.

---

## 1. Primary sources read

### 1.1 Root docs

- `CLAUDE.md` (repo root) - package layering rules, result-type convention, pipeline ordering
  rationale pointer, `PagedResult<T>` explicitly **not** a shipped CommonKernel type.
- `AGENTS.md` (repo root) - one-top-level-public-type-per-file convention, workflow, doc style.

### 1.2 `Interlocutor.CommonKernel`

`src/Interlocutor.CommonKernel/OperationOutcome.cs`, `ResultTypes/*.cs`.

- Zero-dependency package: `IOperationOutcome` marker + `OperationOutcome<TSuccess, ...>` union family
  (arity 0/void through 6), plus case types `Success`, `Failure`, `Failure<TErrorCode>`, `NotFound`,
  `NotFound<TId>`, `Forbidden`, `Conflict`, `Conflicts`, `ValidationError`, `ValidationErrors`,
  `RateLimited`, `Timeout`, `Cancelled`.
- `IOperationOutcome` is the only cross-cutting interface CommonKernel exposes; nothing here knows
  about persistence, repositories, or pagination. Confirms `documentation/glossary.md`'s claim that
  `PagedResult<T>` is deliberately absent - CommonKernel is pure result-type vocabulary, not a general
  "common types" grab-bag.

### 1.3 `Interlocutor.Abstractions`

Full file listing at `src/Interlocutor.Abstractions/**` (Commands, Queries, Core, Notifications,
Pipeline, Markers). Key files read in full:

- `src/Interlocutor.Abstractions/Markers/ITransactionalRequest.cs` - opts a request into the
  ORM-specific `TransactionBehavior`. Doc comment explicitly says enforcement lives in
  `Interlocutor.EntityFrameworkCore`/`Interlocutor.NHibernate`, and that `IExternalStoreRequest`
  requests are excluded to prevent nested transaction scopes.
- `src/Interlocutor.Abstractions/Markers/IExternalStoreRequest.cs` - positive opt-out declaration
  (non-SQL store/outbox), mutually exclusive with `ITransactionalRequest` by convention (ADR-0004),
  not by compiler enforcement.
- `src/Interlocutor.Abstractions/Markers/IAuditableRequest.cs`, `IRequiresPolicy.cs` - same
  marker-interface shape (`request is TMarker` check inside the corresponding behavior, no-op via
  `next()` otherwise).
- Package depends only on CommonKernel + framework DI/Logging abstractions; must never depend on
  AspNetCore or store-specific packages (`CLAUDE.md`, enforced by
  `tests/Interlocutor.Architecture.Tests/DependencyTests.cs`).

### 1.4 EF Core / NHibernate transaction behaviors

- `src/Interlocutor.EntityFrameworkCore/TransactionBehavior.cs` (full file read).
  `TransactionBehavior<TRequest, TResponse, TContext>` takes `Lazy<TContext> db` (`TContext : DbContext`),
  constrained `where TRequest : ITransactionalRequest`. `Handle()`: residual runtime check for
  `IExternalStoreRequest` (constraint can require a marker's presence, never its absence - documented
  in the XML remarks); otherwise begins `BeginTransactionAsync`, runs `next()`, and on a
  classifier-determined success calls `SaveChangesAsync` then `CommitAsync`; on failure or a
  `SaveChangesAsync` exception, `RollbackAsync`.
- `src/Interlocutor.NHibernate/TransactionBehavior.cs` (full file read). Same shape, `Lazy<ISession> session`,
  no generic `TContext`/store type parameter (NHibernate's `ISession` isn't itself generic per-store the
  way `DbContext` subclasses are). `BeginTransaction()` / `CommitAsync()` (implicitly flushes tracked
  changes - no separate `SaveChangesAsync` step, unlike EF Core) / `RollbackAsync()`.
- Both `.csproj` files: single `ProjectReference` to `Interlocutor.Abstractions`, one `PackageReference`
  to their ORM (`Microsoft.EntityFrameworkCore` / `NHibernate`). Both are `IsTrimmable=true`, deliberately
  **not** `IsAotCompatible` - `OperationOutcome<...>` is a value type, so the container can't close
  `IPipelineBehavior<TRequest,TResponse>` over it without a JIT (tracked at `.scratch/aot-support/map.md`).

### 1.5 DI package split (the structural template)

`src/Interlocutor.DependencyInjection.{Abstractions,Autofac,Microsoft}/**` (full listing read; several
files read in full).

- **`.Abstractions`** owns the container-neutral vocabulary: `InterlocutorRegistration`,
  `InterlocutorFactoryRegistration`, `RegistrationLifetime` (enum: `Singleton`/`Scoped`/`Transient` -
  mirrors what every supported container exposes under its own name, so the neutral record never
  references a container-specific type), `RegistrationKind`, `IRootScopeDetector` (interface only - each
  container package supplies its own implementation, because "root scope" has no container-neutral
  answer, only a container-neutral place to ask), `InterlocutorBuilder`/`InterlocutorPipelineBuilder`
  (fluent config surface), `RegistrationApplier`, `RegistrationValidator`. Depends only on
  `Interlocutor.Abstractions` (project reference) - **no** third-party container package reference at all.
- **`.Microsoft`** and **`.Autofac`** are thin: `ServiceCollectionExtensions.cs` /
  `ContainerBuilderExtensions.cs` (the single `AddInterlocutor()` entry point, translating the neutral
  `InterlocutorBuilder` output into container-specific registration calls) plus one
  `{Microsoft,Autofac}RootScopeDetector.cs` implementing `IRootScopeDetector` for that container. Each
  references `Interlocutor.Abstractions` + `Interlocutor.DependencyInjection.Abstractions` (project refs)
  and exactly one container package (`Microsoft.Extensions.DependencyInjection`/`Autofac` +
  `Autofac.Extensions.DependencyInjection`).
- This is the concrete shape to mirror: **one neutral abstractions package with zero third-party
  container/ORM dependency, plus N thin per-implementation packages, each referencing the neutral
  package and exactly one third-party package.**

### 1.6 `tests/Interlocutor.Architecture.Tests/DependencyTests.cs`

Full file read. Current NetArchTest rules (assembly-level, via `Types.InAssembly(...).ShouldNot()/.Should().HaveDependencyOnAny/On(...)`):

1. `CommonKernel` → must not depend on any other Interlocutor package.
2. `Abstractions` → must not depend on AspNetCore/EFCore/FluentValidation/NHibernate.
3. `AspNetCore` → must not depend on EFCore/FluentValidation/NHibernate.
4. `DependencyInjection.Abstractions` → must not depend on AspNetCore/`.Microsoft`/`.Autofac`/EFCore/
   FluentValidation/NHibernate, and (separately, assembly-reference-list check rather than per-type)
   must reference `Interlocutor.Abstractions`.
5. `EntityFrameworkCore`, `FluentValidation`, `NHibernate` → each must depend on `Interlocutor.Abstractions`.
6. `DependencyInjection.Microsoft` → must not depend on `.Autofac`/EFCore/FluentValidation/NHibernate.
7. `DependencyInjection.Autofac` → must not depend on `.Microsoft`/EFCore/FluentValidation/NHibernate.
8. `EntityFrameworkCore` → must not depend on `NHibernate`/`FluentValidation`.
9. `NHibernate` → must not depend on `EntityFrameworkCore`/`FluentValidation`.

Note the store packages (`EntityFrameworkCore`, `NHibernate`) are **not** currently asserted to avoid
depending on `Interlocutor.DependencyInjection.*` - there's no existing rule preventing a store package
from referencing the DI abstractions package, which matters for §4 below (a UoW abstraction needs to be
resolvable by both DI packages, so its natural home is a package the store packages can safely sit next to,
not one the DI packages must reach into the store packages for).

### 1.7 README.md pipeline ordering rationale

`README.md:38` - the canonical order comment `// Logging → Auditing → Authorization → Validation →
Transaction`. Confirmed against `docs/adr/0009-no-default-pipeline.md`: the recommended chain is opt-in
sugar (`UseRecommendedChain()`), never a default (ADR-0009), and the ordering reasoning is "auditing
outside authorization so a denied request is still recorded; transaction innermost so only handler work
is transactional."

### 1.8 `documentation/glossary.md` and related docs

Full file read. Load-bearing entries for this research:

- **"Transaction scope"**: *"One logical request = one DI scope = one scoped `DbContext`/`ISession`.
  `TransactionBehavior` and `ResourceCommandHandler` receive the same scoped session via constructor
  injection. The mediator never opens its own scope - it borrows the ambient one."* This is the existing
  mechanism that already delivers "same transaction and session" - a UoW abstraction has to compose with
  this invariant, not invent a second one.
- **"Lazy resolution"**: `TransactionBehavior` uses `Lazy<TContext>`/`Lazy<ISession>` so non-transactional
  requests never construct the session.
- **"PagedResult<T>" / "IPagedQuery<TItem>"**, under a section explicitly titled *"Pagination - These are
  not shipped types. Interlocutor's core has no opinion about pagination - these rows illustrate one way a
  consumer composes their own paged response as a union payload."* This is the sharpest tension for this
  research (see §3.2).
- **"Packages"** table description of every existing `src/*` package's role, matching what the `.csproj`
  files show.

`documentation/transactions-and-store-integration.md` (full file read) - worked examples of
`TransactionBehavior` for both ORMs, `ITransactionalRequest`/`IExternalStoreRequest` markers,
commit/rollback semantics (including the "no rollback on pipeline short-circuit" rule, since
`TransactionBehavior` never runs before that point), nested-scope dispatch (a handler dispatching another
command via `ISender` shares the *same* scoped session/transaction because the mediator never opens its
own scope), and the outbox-pattern guidance for notification handlers running after commit.

`documentation/mediator-lifetimes-and-scope.md` (full file read) - the mediator is registered scoped and
non-configurable (ADR-0011); one dispatch = one DI scope = one consistent view of scoped dependencies;
root-scope guard throws if the mediator is resolved from the root container; background services must
create an explicit scope per unit of work. This is the DI-lifetime mechanism any new UoW abstraction has
to plug into rather than reinvent.

### 1.9 ADRs read

- `docs/adr/0004-marker-interfaces-for-cross-cutting.md` - marker-interface-as-opt-in decision and its
  stated negative: "Nothing compiler-enforces that the correct commands implement the correct markers."
  Mitigation suggested there (never built, per the DependencyTests.cs read above): an architecture test
  asserting every `ICommand` implements exactly one of `ITransactionalRequest`/`IExternalStoreRequest`.
- `docs/adr/0009-no-default-pipeline.md` - pipeline is empty by default; behaviors are a flat
  builder-call-ordered list filtered by marker constraint; no profiles, no default merge semantics.
- (Also skimmed 0002 pipeline-behavior-onion-model, 0011 mediator-scoped-lifetime-and-root-scope-guard,
  0015 pipeline-composition-behind-interlocutorpipelinebuilder, 0016 trim-compatible-not-aot-compatible -
  no repository/UoW-specific content in any of them; 0011's root-scope guard and 0016's AOT/trim stance
  both constrain the recommendation in §3.6/§4.)

No `docs/adr/` entry currently exists for repository, unit-of-work, or persistence-abstraction patterns -
this would be new ground for the project's ADR log, not a revision of an existing decision.

### 1.10 DI abstraction internals (for the package-shape recommendation)

- `src/Interlocutor.DependencyInjection.Abstractions/RegistrationLifetime.cs` - the container-neutral
  lifetime enum pattern to reuse rather than reinvent if a UoW package needs to describe its own
  registrations neutrally.
- `src/Interlocutor.DependencyInjection.Abstractions/IRootScopeDetector.cs` - the pattern for "one
  interface, container asks the question, per-container package answers it" - directly reusable model
  for any future container-specific behavior a UoW package might need (there isn't an obvious one today,
  but it's the established idiom if one arises).
- `src/Interlocutor.DependencyInjection.Microsoft/ServiceCollectionExtensions.cs` and
  `src/Interlocutor.DependencyInjection.Autofac/ContainerBuilderExtensions.cs` (opening sections read) -
  confirm both packages expose exactly one `AddInterlocutor(..., Action<InterlocutorBuilder> configure)`
  entry point with no legacy overloads, and that `InterlocutorBuilder.Defaults` are applied with try-add
  semantics identically in both containers via `RegistrationApplier`.

---

## 2. External prior art

### 2.1 The legacy tutorial (as named by the task)

[Implementing the Repository and Unit of Work Patterns in an ASP.NET MVC Application](https://learn.microsoft.com/en-us/aspnet/mvc/overview/older-versions/getting-started-with-ef-5-using-mvc-4/implementing-the-repository-and-unit-of-work-patterns-in-an-asp-net-mvc-application)
(EF 5 / MVC 4, 2013, still hosted on Microsoft Learn under "older versions").

- Defines a per-entity repository (`IStudentRepository`: `GetStudents`, `GetStudentByID`, `InsertStudent`,
  `DeleteStudent`, `UpdateStudent`, `Save`), then a `GenericRepository<TEntity>` with `Get(filter, orderBy,
  includeProperties)`, `GetByID`, `Insert`, `Delete` (two overloads), `Update` - `filter`/`orderBy` are
  passed in as `Expression<Func<TEntity,bool>>` / `Func<IQueryable<TEntity>,IOrderedQueryable<TEntity>>`
  lambdas from the caller, not a first-class sort/filter-spec type.
  own change tracking, so one `context.Entry(entity).State = EntityState.Modified` per repository call.
- `UnitOfWork` class: holds a single `SchoolContext` field, exposes one property per repository (each
  lazily constructed with the *same* context instance), and one `Save()` method that calls
  `context.SaveChanges()`. The doc states its purpose in one sentence: *"The unit of work class serves one
  purpose: to make sure that when you use multiple repositories, they share a single database context. That
  way, when a unit of work is complete you can call the SaveChanges method on that instance of the context
  and be assured that all related changes will be coordinated."* This is exactly the "same session" half of
  the hard requirement in this task, achieved purely by constructor-sharing one context instance - there's
  no explicit `BeginTransaction`/`Commit` in the tutorial's `UnitOfWork` at all (relies on `SaveChanges`'s
  own implicit transaction).
- Explicit caveat in the doc itself: *"There are many ways to implement the repository and unit of work
  patterns... The approach shown in this tutorial is one option for you to consider, not a recommendation
  for all scenarios."*
- Also flags a real gotcha directly relevant to any generic `Get()`-style repository method: once
  `IQueryable` is materialized to `IEnumerable` inside the repository, filter/sort/paging done by the
  *caller* afterward runs in memory on the app server, not as SQL - the tutorial shows the generated SQL
  before/after to make this concrete. A repository abstraction that hides `IQueryable` behind an
  `IEnumerable`-returning method silently kills query pushdown unless the filter/sort/paging is threaded
  through the repository's own method signature (as `GenericRepository.Get` does, via lambda parameters).

### 2.2 Modern EF Core / Microsoft's current stance

[Designing the infrastructure persistence layer - .NET Microservices Architecture eBook](https://learn.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/infrastructure-persistence-layer-design)
(still current on Microsoft Learn; part of the .NET Architecture Guides).

- States plainly: **"EF DbContext implements both the Repository and the Unit of Work patterns."** and
  **"In EF, the Unit of Work pattern is implemented by a DbContext and is executed when a call is made to
  SaveChanges."** This is Microsoft's own position that `DbContext` already *is* a UoW - a from-scratch
  repository/UoW layer over EF Core is additive convenience/testability, not filling a functional gap.
- Still recommends a **Repository pattern**, but scoped precisely: **one repository per aggregate root**,
  not per table/entity - *"you should only define one repository for each aggregate root... never create a
  repository for each table in the database"* - because the aggregate root is what owns transactional
  consistency for everything inside it. Recommends constraining a generic base repository's type parameter
  with an `IAggregateRoot` marker interface so the type system itself prevents a repository being created
  for a non-root entity.
- On Unit of Work specifically: *"A unit of work refers to a single transaction that involves multiple
  insert, update, or delete operations... The Unit of Work pattern can be implemented with or without
  using the Repository pattern."* - i.e., UoW and Repository are explicitly presented as **independently
  useful, separable** ideas, not a package deal.
- Includes a direct dissenting voice from Jimmy Bogard (creator of MediatR, cited by name in the doc):
  *"I'm really not a fan of repositories, mainly because they hide the important details of the underlying
  persistence mechanism... Going CQRS meant that we didn't really have a need for repositories any more."*
  Given Interlocutor is itself a CQRS/mediator library, this is a directly relevant counterpoint to weigh -
  worth surfacing explicitly rather than treating "add repositories" as an uncontested improvement.
- Bottom line stated by the doc itself, in a subsection literally titled "Repositories shouldn't be
  mandatory": repositories are useful but not essential to a good DDD/CQRS design; "use the Repository
  pattern or not, as you see fit."

### 2.3 NHibernate ISession/ITransaction as the session+transaction unit

Official reference: [Chapter 12. Transactions And Concurrency](https://nhibernate.info/doc/nhibernate-reference/transactions.html).

- `ISession` is itself framed as the unit-of-work object: *"An ISession is an inexpensive, non-threadsafe
  object that should be used once, for a single business process, and then discarded."*
- Session-per-request is the documented default web pattern: *"Each call to a service method could create
  a new ISession, Flush() it, Commit() its transaction, Close() it and finally discard it."* This matches
  exactly what `documentation/mediator-lifetimes-and-scope.md` and the glossary's "Transaction scope" entry
  already describe for Interlocutor: one DI scope, one session, shared across the whole dispatch.
  the ADO.NET connection while waiting on user interaction, then reconnect - not relevant to
  Interlocutor's synchronous-per-dispatch model but worth naming as the one place NHibernate's own docs
  describe a UoW *longer* than one DI scope.
- Explicit warning against skipping transactions even for reads: avoiding `ITransaction` "degrades
  performance and disables features like auto-flush and second-level caching" - reinforces that
  `TransactionBehavior`'s existing "only requests implementing `ITransactionalRequest` get a transaction"
  policy is a deliberate opt-in tradeoff, not a missing feature to route around silently in a new UoW type.
- Concurrency caution, corroborated by community sources found in the same search: never share one
  `ISession`/`ITransaction` across concurrent threads or open more than one per connection - directly
  relevant to "everything must be properly scoped" in the task's hard requirement, since a
  singleton-lifetime UoW wrapper would violate this outright.

### 2.4 A pluggable-persistence CQRS/mediator framework's take: ABP Framework

[ABP Framework - Repositories](https://abp.io/docs/latest/framework/architecture/domain-driven-design/repositories) and
[ABP Framework - Unit of Work](https://abp.io/docs/latest/framework/architecture/domain-driven-design/unit-of-work).

Chosen as the external example because ABP, like Interlocutor, ships store-agnostic abstractions with
separate EF Core / MongoDB implementation packages, which is structurally the closest published analog to
"mirror the DI package split for persistence."

- `IRepository<TEntity, TKey>` shape: `GetAsync`/`FindAsync` (single entity, by id or predicate - the
  `Get`/`Find` naming split maps directly onto Interlocutor's own `NotFound` vs. non-`NotFound` outcome
  distinction), `GetListAsync` (all), **`GetPagedListAsync(skipCount, maxResultCount, sorting, ...)`** (the
  closest published equivalent to what this task calls "paging with sorting/filtering" as a first-class
  repository method rather than a caller-supplied lambda), `GetCountAsync`, `InsertAsync`/`UpdateAsync`/
  `DeleteAsync` plus bulk `*ManyAsync` variants, and an escape hatch `GetQueryableAsync()` returning
  `IQueryable<TEntity>` for ad hoc LINQ composition when the fixed CRUD surface isn't enough.
- `IUnitOfWork`: `Repository<T>()`, `SaveChangesAsync()`, `BeginTransactionAsync()`,
  `CommitTransactionAsync()`, `RollbackTransactionAsync()`. ABP's application-service base class wraps
  every service method in a UoW by default (automatic transaction + automatic rollback on unhandled
  exception) - i.e., ABP's UoW is ambient/automatic per logical operation, conceptually the same shape as
  Interlocutor's own `ITransactionalRequest`/`TransactionBehavior` pairing, just generalized to also gate
  repository resolution instead of only wrapping the pipeline.
- Notably, ABP's `GetPagedListAsync` takes `sorting` as a **string** (an OData/SQL-`ORDER BY`-like
  expression), not a typed sort-spec object, and has no first-class filter-spec parameter at all beyond
  what `GetQueryableAsync()` lets a caller LINQ against directly - even a mature, widely-used framework
  doesn't standardize a typed filter DSL; it either falls back to `IQueryable` escape hatches or leaves
  filtering to caller-composed predicates. This is evidence against inventing a bespoke `FilterSpec` type
  in a new Interlocutor package without a much stronger reason.

---

## 3. Synthesis

### 3.1 Where new shared abstractions should live

The existing package-layering rule (`CLAUDE.md`, `documentation/glossary.md` "Packages" table,
`DependencyTests.cs`) is unambiguous about *why* CommonKernel and Abstractions are split the way they are:
CommonKernel is zero-dependency pure result-type vocabulary; Abstractions adds the mediator/handler/pipeline
concepts and is allowed framework DI/Logging dependencies but nothing store- or HTTP-specific. A Unit of
Work / Repository abstraction doesn't fit cleanly into either:

- It isn't a result type (doesn't belong in CommonKernel).
- It isn't mediator/pipeline/dispatch machinery (bolting `IUnitOfWork`/`IDomainRepository<T>` directly onto
  `Interlocutor.Abstractions` would mix "how a request gets routed and cross-cut" with "how a handler talks
  to storage" - two concerns the current package boundaries keep apart on purpose, and it would force every
  consumer of `Interlocutor.Abstractions`, including AspNetCore-only or FluentValidation-only consumers, to
  carry persistence-shaped types they don't use).

The DI-package split is the closer analogy the task explicitly asks to mirror, and it already solves this
exact shape of problem: a small **neutral abstractions package with zero third-party dependency**
(`Interlocutor.DependencyInjection.Abstractions`, ProjectReference only to `Interlocutor.Abstractions`),
plus **N thin per-implementation packages** (`.Microsoft`, `.Autofac`), each referencing the neutral package
plus exactly one third-party package. A persistence abstraction should copy that shape one level over:

- **`Interlocutor.Persistence.Abstractions`** (name is illustrative, not a proposal to bikeshed here) -
  container-neutral, ORM-neutral interfaces (`IUnitOfWork`, `IDomainRepository<TEntity, TKey>` or similar),
  referencing only `Interlocutor.Abstractions` (for the marker-interface idiom and pipeline types it needs
  to interoperate with) and `Interlocutor.CommonKernel` transitively. Zero EF Core / NHibernate / paging-
  library dependency, same as `.DependencyInjection.Abstractions` has zero Autofac/Microsoft DI dependency.
- **`Interlocutor.Persistence.EntityFrameworkCore`** and **`Interlocutor.Persistence.NHibernate`** (or
  these could plausibly live *inside* the existing `Interlocutor.EntityFrameworkCore` /
  `Interlocutor.NHibernate` packages rather than as new ones - see the open question in §5) - implement the
  neutral interfaces against `DbContext` / `ISession`, each referencing the new abstractions package plus
  their one ORM package, exactly like `TransactionBehavior` does today.

This also keeps the existing rule *"`Interlocutor.EntityFrameworkCore` must not depend on
`Interlocutor.NHibernate`"* (and vice versa) trivially satisfiable, because neither store package would ever
need to reference the other - they'd both depend downward on the one neutral persistence-abstractions
package, mirroring exactly how `.DependencyInjection.Microsoft` and `.DependencyInjection.Autofac` both
depend downward on `.DependencyInjection.Abstractions` without depending on each other.

### 3.2 Type shapes - and the `PagedResult<T>` tension

CLAUDE.md is explicit and current-fact, not a proposal: *"`PagedResult<T>` is not a shipped type here -
it's a consumer-defined success-case type, per `documentation/glossary.md`."* The glossary itself frames
the whole Pagination section as "Interlocutor's core has no opinion about pagination." That is a deliberate
design stance, not an oversight - it keeps `OperationOutcome<TSuccess,...>`'s success-case type fully
open, so a consumer's paged response shape participates in the same union-based result model as everything
else, rather than Interlocutor imposing one.

A `IDomainRepository<TEntity,TKey>` with a `GetPagedAsync(...)` method needs *some* return shape for a page
of results plus total count/sort/filter metadata - that's unavoidable if "paging with sorting/filtering" is
a first-class repository capability at all (both the legacy tutorial's `Get(filter, orderBy, ...)` and
ABP's `GetPagedListAsync(skipCount, maxResultCount, sorting)` land on exactly this same needed shape). This
is a genuine, real tension between two currently-true statements, not something to silently paper over by
either:

- shipping a `PagedResult<T>` in a new package anyway (which would contradict CLAUDE.md's current framing
  of pagination as deliberately out of core's opinion, unless that framing is itself revisited), or
- omitting paging metadata from the abstraction entirely (which would fail the task's explicit requirement
  that the abstraction cover "paging with sorting/filtering").

Two non-code-writing ways this tension could resolve, both consistent with what's on record today, are
named here as the two live options rather than picked between:

1. **Paging types live in the persistence-abstractions package, not CommonKernel.** `PagedResult<T>` stays
   absent from CommonKernel exactly as CLAUDE.md says; a new `Interlocutor.Persistence.Abstractions`
   package - which by definition doesn't exist today, so nothing currently says it can't have an opinion -
   defines its own paging/sort/filter types as the *repository* abstraction's vocabulary, distinct from the
   *result-type* vocabulary CommonKernel and the glossary's Pagination section are talking about. A
   consumer's `OperationOutcome<PagedResult<T>, NotFound>` handler return type still composes exactly as
   before, whether `PagedResult<T>` comes from this new package or the consumer's own code.
2. **The repository abstraction stays paging-primitive-only** (skip/take or page/pageSize plus a sort
   parameter as ABP's does, via a string or a minimal typed enum-based spec) **and leaves materializing a
   `PagedResult<T>`-shaped success case entirely to the consumer**, exactly as query handlers do today with
   no repository layer at all. This keeps CommonKernel's stance completely untouched but pushes some of the
   "why bother with the abstraction" value proposition back onto the consumer.

Either way, whichever choice is made needs to update `CLAUDE.md`'s pagination sentence and the glossary's
Pagination section explicitly, because both currently describe present-tense fact ("is not a shipped type
here"), and a persistence package shipping paging types - even in a *different* package than CommonKernel -
is close enough to that sentence's spirit that leaving it unedited would read as contradicting the new
package's own existence to a future reader who doesn't have this research doc in front of them.

Beyond paging, the shapes a persistence-abstractions package would need to define (illustrative names only):

- **`IUnitOfWork`** - `SaveChangesAsync(CancellationToken)` at minimum; whether it also exposes
  `BeginTransactionAsync`/`CommitAsync`/`RollbackAsync` directly (ABP's shape) or leaves transaction
  begin/commit/rollback entirely to the pipeline behavior (Interlocutor's current `TransactionBehavior`
  shape) is the central design question in §3.3.
- **`IDomainRepository<TEntity, TKey>`** (or `IRepository<TEntity,TKey>`, matching the more common external
  naming in §2) - CRUD (`GetAsync`/`FindAsync`, `InsertAsync`, `UpdateAsync`, `DeleteAsync`), a "get many"
  method, and a "get paged" method taking whatever sort/filter primitives are settled by the choice above.
  Whether it's constrained to an `IAggregateRoot`-style marker (as the eShopOnContainers doc recommends) is
  itself an open design question - Interlocutor doesn't currently have any aggregate-root concept anywhere
  in the codebase, so introducing one is a bigger step than it looks and should be flagged as such rather
  than assumed.
- **`IDomainService`** as named in the task is the vaguest of the three asked-for shapes and has no direct
  analog read in any of the internal or external sources above (`ResourceCommandHandler`'s
  load→authorize→handle template method is the closest existing internal concept, but it's a *handler*
  base class, not a persistence-facing service) - this needs its own scoping conversation before a shape is
  proposed, not a shape invented here to fill the gap.

### 3.3 Interaction with `ITransactionalRequest` / `TransactionBehavior`

The existing model already delivers the "same transaction and same session" half of the hard requirement,
by construction, not by convention: the mediator is registered scoped and never opens its own scope
(`documentation/mediator-lifetimes-and-scope.md`, ADR-0011); `TransactionBehavior` and any handler in the
same dispatch resolve the *same* scoped `Lazy<TContext>`/`Lazy<ISession>` instance
(`documentation/glossary.md`'s "Transaction scope" entry, corroborated by the nested-dispatch example in
`documentation/transactions-and-store-integration.md` where a handler-dispatched sub-command shares the
parent's transaction for free, with no code doing anything special to make that true).

Given that, a Unit-of-Work abstraction has three live options, not one obvious answer:

1. **UoW is a façade over the same scoped `DbContext`/`ISession`, not a new transaction owner.**
   `IUnitOfWork.SaveChangesAsync()` on EF Core forwards straight to the scoped `DbContext.SaveChangesAsync`
   (or on NHibernate, `ISession.Flush`); it never opens its own `BeginTransactionAsync`. Transaction
   begin/commit/rollback stays exactly where it is today, inside `TransactionBehavior`, driven by
   `ITransactionalRequest` + the outcome classifier. This is the option requiring the least new surface
   and the smallest blast radius against everything §1.4/§1.8 already documents, since it changes nothing
   about *when* a transaction opens or closes - it just gives handlers a narrower, repository-shaped
   interface instead of a raw `Lazy<DbContext>`/`Lazy<ISession>`.
2. **UoW replaces `TransactionBehavior` as the transaction owner.** A `UnitOfWorkBehavior` (or the existing
   `TransactionBehavior` itself, modified) resolves the same scoped `IUnitOfWork`, begins/commits/rolls back
   through it instead of talking to `DbContext`/`ISession` directly, and `IDomainRepository<T>` instances
   ask the ambient `IUnitOfWork` for their session/context rather than each repository resolving its own
   `Lazy<TContext>`. This is closer to ABP's shape (`IUnitOfWork.BeginTransactionAsync`/
   `CommitTransactionAsync`) and to what the task's "operations within a Unit of Work MUST be handled
   within the same transaction and session" wording most literally suggests, but it's a materially bigger
   change: it means the `TransactionBehavior` contract stated in `documentation/glossary.md` and
   `documentation/transactions-and-store-integration.md` today would need to change or be superseded, which
   the task does *not* ask for and which would need its own careful compatibility discussion (existing
   consumers already depend on `TransactionBehavior<TRequest,TResponse,TContext>`'s exact shape).
3. **UoW sits beside `ITransactionalRequest` as a second, independent opt-in marker**, for the narrower case
   of a handler that wants repository-shaped access without necessarily being transactional (e.g., only
   `IDomainRepository<T>.GetPagedAsync` for a query handler, which per
   `documentation/transactions-and-store-integration.md` "Queries never have transactions" - a query handler
   still needs *a* session to read from, just never a transaction). This treats "gets a repository" and
   "is transactional" as orthogonal opt-ins, matching ADR-0004's own general marker-interface philosophy
   (opt-in is visible at the request type's declaration) rather than conflating them.

Option 1 is the one that changes the least about what's already documented and tested; options 2 and 3 are
real alternatives worth a deliberate choice, not dismissed here - flagged for the actual design discussion.

Whichever option is chosen, the "everything must be properly scoped" half of the hard requirement is
already the established idiom to follow, not a new problem: `IUnitOfWork` and `IDomainRepository<T>`
implementations must be registered **scoped** (matching `DbContext`/`ISession`'s own registration lifetime,
per `documentation/transactions-and-store-integration.md`'s own registration examples,
`services.AddDbContext<AppDbContext>(..., ServiceLifetime.Scoped)` and
`services.AddScoped<ISession>(...)`), and must use the same `Lazy<T>` deferred-resolution idiom
`TransactionBehavior` already uses so a non-transactional/non-repository-using dispatch never pays for
session construction it doesn't need.

### 3.4 Package names mirroring the DI split

Concrete proposal (illustrative naming, matching the existing `Interlocutor.<Area>.<Implementation>`
convention seen in `Interlocutor.DependencyInjection.{Abstractions,Autofac,Microsoft}`):

| Package | Contents | References |
|---|---|---|
| `Interlocutor.Persistence.Abstractions` | `IUnitOfWork`, `IDomainRepository<TEntity,TKey>`, paging/sort/filter primitive types (per the choice in §3.2), any new marker interface from §3.3 option 3 | `Interlocutor.Abstractions` (project ref) only |
| `Interlocutor.Persistence.EntityFrameworkCore` | `DbContext`-backed implementations of the above | `Interlocutor.Persistence.Abstractions` + `Microsoft.EntityFrameworkCore` |
| `Interlocutor.Persistence.NHibernate` | `ISession`-backed implementations of the above | `Interlocutor.Persistence.Abstractions` + `NHibernate` |

An equally defensible alternative is folding the EF Core/NHibernate implementations into the *existing*
`Interlocutor.EntityFrameworkCore`/`Interlocutor.NHibernate` packages instead of new
`Interlocutor.Persistence.*` packages, since those packages already own the ORM-specific
`TransactionBehavior` and already reference exactly the one ORM package each would need. This trades a
cleaner "persistence" naming grouping against not spinning up two more packages/csproj/test-project pairs
for what might be a small amount of additional code. Named as an open question in §5 rather than settled
here.

### 3.5 `DependencyTests.cs` additions

New assertions, following the exact pattern of the existing ones in the same file:

- `Interlocutor.Persistence.Abstractions` must not depend on `Interlocutor.AspNetCore`,
  `Interlocutor.EntityFrameworkCore`/`.Persistence.EntityFrameworkCore`, `Interlocutor.FluentValidation`,
  `Interlocutor.NHibernate`/`.Persistence.NHibernate`, or either `Interlocutor.DependencyInjection.*`
  package - mirroring `DependencyInjectionAbstractions_ShouldDependOnlyOnAbstractions`.
- `Interlocutor.Persistence.EntityFrameworkCore` and `Interlocutor.Persistence.NHibernate` must each depend
  on `Interlocutor.Persistence.Abstractions` (mirroring `ExampleImplementations_ShouldDependOnAbstractions`),
  and must not depend on each other (mirroring
  `EntityFrameworkCore_ShouldNotDependOnNHibernateOrFluentValidation` /
  `NHibernate_ShouldNotDependOnEntityFrameworkCoreOrFluentValidation`).
- If the implementations instead fold into the existing store packages (§3.4's alternative), the existing
  rules 8/9 in §1.6 (EF Core ⇎ NHibernate) already cover the "don't depend on each other" half for free;
  only a new "must depend on `Interlocutor.Persistence.Abstractions`" assertion would be needed.

### 3.6 Open questions / risks (see also §5)

Carried into §5 rather than duplicated here.

---

## 4. Summary table: what each source actually establishes

| Claim | Source |
|---|---|
| `PagedResult<T>` is not a shipped CommonKernel type today | `CLAUDE.md` (repo root); `documentation/glossary.md` §Pagination |
| One dispatch = one DI scope = one scoped `DbContext`/`ISession`, shared by `TransactionBehavior` and every handler in that dispatch | `documentation/glossary.md` §Transaction Model "Transaction scope"; `documentation/mediator-lifetimes-and-scope.md` §Scoped Lifetime |
| `TransactionBehavior` uses `Lazy<T>` so non-transactional requests never resolve a session | `src/Interlocutor.EntityFrameworkCore/TransactionBehavior.cs`; `src/Interlocutor.NHibernate/TransactionBehavior.cs`; `documentation/glossary.md` "Lazy resolution" |
| `ITransactionalRequest`/`IExternalStoreRequest` are meant to be mutually exclusive by convention, not compiler-enforced | `docs/adr/0004-marker-interfaces-for-cross-cutting.md`; corroborated in both `TransactionBehavior.Handle()` XML remarks |
| The DI package split is neutral-package + N thin per-container packages, each referencing the neutral package plus exactly one third-party package, with zero cross-container-package dependency | `src/Interlocutor.DependencyInjection.{Abstractions,Autofac,Microsoft}/*.csproj`; `tests/Interlocutor.Architecture.Tests/DependencyTests.cs` |
| The pipeline is empty by default; the recommended Logging→Auditing→Authorization→Validation→Transaction chain is opt-in sugar, never a default | `docs/adr/0009-no-default-pipeline.md`; `README.md:38` |
| EF Core `DbContext` already implements both Repository and Unit of Work | [Microsoft Learn - Designing the infrastructure persistence layer](https://learn.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/infrastructure-persistence-layer-design) |
| Recommended repository granularity is one per aggregate root, not per table/entity | Same source, §"Define one repository per aggregate" |
| Repositories are explicitly framed as optional, not essential, even in DDD/CQRS designs; MediatR's author is quoted against them | Same source, §"Repositories shouldn't be mandatory" |
| NHibernate's `ISession` is itself the unit-of-work object; session-per-request is the documented default web pattern | [NHibernate reference - Transactions And Concurrency](https://nhibernate.info/doc/nhibernate-reference/transactions.html) |
| `ISession`/`ITransaction` must never be shared across concurrent threads or more than one per connection | Same source; corroborated by community sources found alongside it |
| A mature store-agnostic repository abstraction (ABP) uses `skipCount`/`maxResultCount`/string `sorting` for paging, with `IQueryable` as the escape hatch rather than a typed filter DSL | [ABP Framework - Repositories](https://abp.io/docs/latest/framework/architecture/domain-driven-design/repositories) |
| A UoW pattern in an ambient-DI CQRS-adjacent framework (ABP) wraps every service method by default and exposes explicit `BeginTransactionAsync`/`CommitTransactionAsync`/`RollbackTransactionAsync` | [ABP Framework - Unit of Work](https://abp.io/docs/latest/framework/architecture/domain-driven-design/unit-of-work) |
| The original repository-hides-`IQueryable` gotcha (filter/sort/paging silently moving from SQL to in-memory) is a real, documented failure mode of a naive generic repository | [Legacy EF5/MVC4 tutorial](https://learn.microsoft.com/en-us/aspnet/mvc/overview/older-versions/getting-started-with-ef-5-using-mvc-4/implementing-the-repository-and-unit-of-work-patterns-in-an-asp-net-mvc-application) |

---

## 5. Recommendation

This section is a synthesis of the above, not a decision - no code was written and no ADR was filed as part
of this research task.

1. **House the abstraction in a new, low/zero-dependency package** (`Interlocutor.Persistence.Abstractions`
   or similar), structured exactly like `Interlocutor.DependencyInjection.Abstractions`: a project reference
   to `Interlocutor.Abstractions` only, no ORM/container dependency. Do not bolt `IUnitOfWork`/
   `IDomainRepository<T>` onto `Interlocutor.Abstractions` itself - that package's whole point per
   `CLAUDE.md` is mediator/handler/pipeline concepts, and every consumer of AspNetCore- or
   FluentValidation-only integration would otherwise inherit persistence-shaped types they never use.
2. **Ship EF Core and NHibernate implementations as their own packages** mirroring
   `.DependencyInjection.{Autofac,Microsoft}`'s "thin, one-ORM-dependency-each" shape - or, as a real
   alternative worth weighing before committing, fold them into the *existing*
   `Interlocutor.EntityFrameworkCore`/`Interlocutor.NHibernate` packages that already own the
   `TransactionBehavior` for each store. Both are structurally sound; the choice is a naming/package-count
   tradeoff, not an architecture question, and is left open (§3.4).
3. **Prefer composing the UoW as a façade over the existing scoped `DbContext`/`ISession`** (§3.3, option 1)
   rather than replacing `TransactionBehavior` as the transaction owner. This satisfies the "same
   transaction and session" hard requirement using the mechanism that already guarantees it today (scoped
   mediator, never opens its own scope, one scoped session per dispatch) instead of introducing a second,
   parallel transaction-ownership model that the existing `TransactionBehavior` docs, tests, and consumer
   contracts would then need to reconcile against. If the team wants `IUnitOfWork` to own
   begin/commit/rollback directly (closer to ABP's shape), that's option 2 - flagged as viable but bigger
   and requiring its own compatibility review of `TransactionBehavior`'s current public contract.
4. **Resolve the `PagedResult<T>` tension explicitly, in writing, before implementation starts** (§3.2):
   either the new persistence-abstractions package owns its own paging/sort/filter vocabulary (distinct
   from, and not contradicting, CommonKernel's continued absence of one), or the repository abstraction
   exposes only paging *primitives* and leaves the paged-response shape to the consumer, exactly as query
   handlers do today. Whichever is chosen, `CLAUDE.md` and `documentation/glossary.md`'s Pagination section
   need an explicit update pass so they don't read as contradicting the new package's existence.
5. **Register everything scoped, resolved via `Lazy<T>`**, matching `TransactionBehavior`'s existing idiom -
   this is not a new problem to solve, it's the established pattern to reuse, and it's also what NHibernate's
   own docs (§2.3) and the concurrency caution around `ISession` demand independently of anything
   Interlocutor-specific.
6. **Treat `IDomainService` as unscoped by this research** - none of the internal or external sources gave it
   a concrete shape distinct from either a plain handler or `ResourceCommandHandler`'s existing
   load→authorize→handle template. It needs its own follow-up conversation about what it's *for* before a
   type shape is proposed.
7. **Add `DependencyTests.cs` rules mirroring the existing DI-package rules** (§3.5) as part of whatever
   package split is chosen, so the new layering is enforced the same way every other package boundary in
   this repo already is.
8. **Weigh Jimmy Bogard's dissent (§2.2) directly rather than skip past it.** Interlocutor is already a
   CQRS/mediator library where handlers can talk to `DbContext`/`ISession` directly via `Lazy<T>` injection
   today, with no repository layer, and that's a coherent, working, currently-shipped design. Adding a
   repository/UoW layer is worth doing for the reasons in this doc (testability, consistent CRUD/paging
   surface, structural parity with the DI split) - but it should be positioned as an **additional, optional**
   package a consumer opts into, never as implying the direct-`DbContext`/`ISession`-injection path
   documented today in `documentation/transactions-and-store-integration.md` was wrong or is being replaced.

## 6. Open Questions

- **Package boundary**: new `Interlocutor.Persistence.{EntityFrameworkCore,NHibernate}` packages, or fold
  into the existing `Interlocutor.EntityFrameworkCore`/`Interlocutor.NHibernate` packages? (§3.4)
- **UoW vs. `TransactionBehavior` relationship**: façade over the existing scoped session (§3.3 option 1,
  smallest change), replace `TransactionBehavior` as transaction owner (§3.3 option 2, biggest change and a
  breaking change to `TransactionBehavior`'s documented contract), or a second independent marker interface
  for "wants a repository" orthogonal to "is transactional" (§3.3 option 3)?
- **`PagedResult<T>` / paging vocabulary**: new package owns its own paging types (distinct from
  CommonKernel), or repository exposes only paging primitives and leaves the response shape to the consumer?
  Either way, `CLAUDE.md` and the glossary's Pagination section need a coordinated edit. (§3.2)
- **Sort/filter spec shape**: no source surveyed here (legacy tutorial, ABP) uses a typed filter-spec DSL -
  both fall back to caller-supplied lambdas/`IQueryable` or a plain sort string. Is inventing a typed
  `SortSpec`/`FilterSpec` in Interlocutor actually warranted, or should the repository's paging method take
  the same kind of escape-hatch shape (`IQueryable<TEntity>` projection, or a caller-supplied
  `Expression<Func<TEntity,bool>>`) that both prior-art sources converged on independently?
- **Aggregate-root concept**: the eShopOnContainers guidance recommends constraining a repository's
  `TEntity` to an `IAggregateRoot` marker. Interlocutor has no aggregate-root concept anywhere in the
  codebase today - introducing `IDomainRepository<T>` without one risks a repository-per-table anti-pattern
  the same Microsoft doc explicitly warns against; introducing one is a materially bigger design surface
  than "just the repository interface."
- **`IDomainService` shape**: entirely unscoped by this research (§5 point 6) - needs its own follow-up
  before any type is proposed.
- **AOT/trim**: every existing store/DI package in this repo is `IsTrimmable=true` but deliberately *not*
  `IsAotCompatible`, because `OperationOutcome<...>` is a value type the container can't close a generic
  pipeline behavior over without a JIT (see `.scratch/aot-support/map.md`,
  `docs/adr/0016-trim-compatible-not-aot-compatible.md`). A generic `IDomainRepository<TEntity,TKey>`
  resolved from DI would need the same trim/AOT posture verified, not assumed inherited for free.
- **Async disposal**: none of the internal transaction behaviors currently implement `IAsyncDisposable`
  themselves (they `await using`/`using` the transaction object, not the context/session). Whether a new
  `IUnitOfWork` needs to be `IAsyncDisposable` itself, and how that interacts with a scoped DI container's
  own disposal of the underlying `DbContext`/`ISession`, wasn't resolved by any source read here.
- **Ambient vs. explicit UoW scope**: everything in §1.8/§2.3 supports "one DI scope = one UoW,
  no exceptions" for the synchronous-per-dispatch case Interlocutor already has. Whether a
  session-per-conversation-style longer-lived UoW (NHibernate's own docs name this as a *different* pattern
  from session-per-request, §2.3) is ever wanted is unaddressed by anything read in this research and would
  need its own justification if raised later.
- **Testability**: none of the internal sources read here include an existing pattern for
  mocking/faking `DbContext`/`ISession` for handler unit tests (`documentation/testing.md` wasn't read in
  full as part of this pass - only glossary/transactions/lifetimes docs were). Before finalizing
  `IDomainRepository<T>`'s shape, `documentation/testing.md` and the existing test projects' mocking
  conventions (NSubstitute, per `CLAUDE.md`) should be reviewed for how they currently handle store mocking,
  since a repository abstraction's main selling point over direct `DbContext` injection is usually
  testability, and that claim should be checked against how this repo tests today rather than assumed.
