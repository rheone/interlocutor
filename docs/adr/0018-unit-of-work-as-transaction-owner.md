# ADR-0018: Unit of Work as Transaction Owner

**Status:** Accepted

## Context

[ADR-0017](0017-optional-persistence-abstraction-repository-and-unit-of-work.md) established `IUnitOfWork` as a façade over the same scoped `DbContext`/`ISession` that `TransactionBehavior` (`Interlocutor.EntityFrameworkCore`/`Interlocutor.NHibernate`) already manages, deliberately not owning transaction begin/commit/rollback - explicitly rejecting the alternative of making `IUnitOfWork` the transaction owner, "for no identified benefit over the façade option."

A concrete benefit has since been identified: a single vocabulary for storage work instead of two (markers + `TransactionBehavior` vs. façade `IUnitOfWork`), a transaction boundary that binds to the first actual write instead of a declared marker plus pipeline position, and a documented, expandable path for NHibernate/EF Core consumers to reach ORM-specific capability beyond the current `IQuery`-only surface. This ADR reverses ADR-0017's specific call on transaction ownership; nothing else in ADR-0017 is revisited.

Two facts already on record still constrain the design:

- `documentation/glossary.md`'s "Transaction scope" entry: one dispatch = one DI scope = one scoped `DbContext`/`ISession`, available for the whole pipeline regardless of transaction state.
- [ADR-0009](0009-no-default-pipeline.md): nothing cross-cutting runs unless the consumer's own builder registers it. This ADR does not amend that guarantee.

## Decision

Make `IUnitOfWork` the transaction owner. `TransactionBehavior` (EF Core and NHibernate) is retired; a new pipeline behavior, registered the same opt-in way `TransactionBehavior` is today, owns transaction lifecycle instead.

### Begin on first write, not on dispatch start

The scoped session is available for the entire pipeline from dispatch start, unchanged from today. The *transaction* opens lazily, the first time anything - `IDomainRepository`, `IUnitOfWork`, or a directly-injected `Lazy<TContext>`/`Lazy<ISession>` - performs a write, not on the first potential read. A dispatch that only ever reads never pays for an open transaction. Reads earlier in the pipeline (permission checks, validation lookups) run against the same session and see a consistent view of anything already staged, without needing a transaction open around them.

This relies on optimistic concurrency being sufficient for the ordinary case, with pessimistic locking left as a documented rare-case escape hatch (see "Baseline vs. advanced surfaces," below) for the read-then-write scenario where it isn't.

### `ITransactionalRequest` narrows, doesn't disappear

`ITransactionalRequest` no longer means "this request wants a transaction" - every write already gets one, automatically, via begin-on-first-write. It narrows to: "this request's read(s) must be protected by the same transaction as its later write(s), from pipeline start" - i.e. it forces an eager transaction begin instead of the default begin-on-first-write, for the check-then-act case (for example, confirming a value is still unique immediately before an insert) where plain begin-on-first-write leaves a window between the protective read and the write.

`IExternalStoreRequest` keeps its current meaning and mechanism unchanged: a request implementing it never has a local transaction opened around it, regardless of whether it happens to touch the scoped session. This isn't redundant with begin-on-first-write's own laziness (a request that never touches the session already avoids a transaction for free) - `IExternalStoreRequest` additionally guarantees no local transaction even if the handler does touch the session, which laziness alone cannot promise.

### Whole-session protection, not abstraction-only

The commit/rollback gate detects a write regardless of access path - through `IDomainRepository`, through `IUnitOfWork`, or through the directly-injected `Lazy<TContext>`/`Lazy<ISession>` escape hatch ADR-0017 preserved. This matches `TransactionBehavior`'s existing guarantee today (it transacts the whole scoped session, not gated by which API touched it); narrowing to abstraction-only protection would be a quiet regression for any handler using the direct-injection path.

For EF Core this falls out for free: nothing reaches the database before `SaveChangesAsync` runs, regardless of how entities were staged, so wrapping that call catches every write uniformly.

For NHibernate this is not free: `ISession`'s default flush mode can auto-flush pending changes before a query executes, which could write to the database ahead of any transaction this behavior tries to open around it. The new NHibernate unit-of-work implementation defaults the session to a non-auto flush mode so nothing can flush before the transaction opens - this is the one piece of richer NHibernate access that ships as part of this ADR rather than being deferred, because whole-session protection depends on it.

### `Cancel` on `IUnitOfWork`

`IUnitOfWork` gains an explicit `Cancel` operation alongside its existing `SaveChangesAsync`: a rare-case escape hatch for a handler that wants to force a rollback without being obligated to also return a classified failure-shaped outcome. `Cancel` rolls back immediately and returns control to the handler - it is not a short-circuit primitive; the handler decides what `OperationOutcome` to return afterward, and the pipeline's automatic commit gate simply finds nothing pending to commit at completion. This keeps `Cancel` a plain unit-of-work operation, consistent with the library's existing preference for outcome values over exception-driven control flow.

Automatic commit at pipeline completion is unchanged in kind from today's classifier-driven rule: commit if the classifier reports success, roll back on a classified failure, on an already-cancelled unit of work, or if the dispatch short-circuited before any write occurred (nothing to roll back in that case, same as today).

### Commit gate stays an explicitly-registered behavior

The new commit/rollback gate is an ordinary, opt-in pipeline behavior, added the same way `TransactionBehavior` is registered today - unconditional for every request once registered (no marker required to participate, the same shape as `LoggingBehavior`), but still absent entirely from a bare `AddInterlocutor()` call. This is a deliberate continuity with [ADR-0009](0009-no-default-pipeline.md): "what runs on a request is fully readable from the builder configuration" is not being amended by this decision.

### `Interlocutor.EntityFrameworkCore` and `Interlocutor.NHibernate` are retired

Both packages' only shipped type is `TransactionBehavior`. With that type superseded, the packages have no remaining reason to exist. The new commit-gate behavior ships from `Interlocutor.Persistence.EntityFrameworkCore`/`Interlocutor.Persistence.NHibernate` instead, alongside the `IUnitOfWork`/`IDomainRepository<T>` implementations already there. `tests/Interlocutor.Architecture.Tests/DependencyTests.cs` loses the rules asserting `Interlocutor.EntityFrameworkCore`/`.NHibernate`'s shape and gains equivalent rules for the new behavior's home in the `Persistence.*` packages. No migration shim ships, consistent with this project's existing pre-1.0 stance (see ADR-0009's own consequences) of accepting a breaking change over carrying a compatibility layer nobody asked for.

### Baseline vs. advanced surfaces - pattern only, mostly deferred

`IUnitOfWork`/`IDomainRepository<T>` (`Interlocutor.Persistence.Abstractions`) stay store-agnostic and minimal: begin-on-first-write session, `SaveChangesAsync`, `Cancel`, CRUD, paging, the `IQueryable` escape hatch. Each store package may add whatever additional members its ORM does well, directly on its concrete `IUnitOfWork`/`IDomainRepository<T>` implementation - not mirrored onto the other store package, not added to the shared interface. This mirrors the existing `Interlocutor.DependencyInjection.{Abstractions,Autofac,Microsoft}` split: one small neutral contract, provider packages shaped by what their underlying technology actually does well.

This ADR commits to the *pattern* and to the one member already made load-bearing above (NHibernate's flush-mode default). It does not commit to a concrete list of further advanced members - explicit lock modes, a stateless-session path, HQL/native SQL/Criteria access, `ExecuteUpdate`/`ExecuteDelete`-style bulk helpers, cache/tracking control (`Evict`/`ChangeTracker.Clear`), and multi-query batching were all discussed as plausible future additions, but none were pressure-tested to a concrete shape. Deferred to future, focused ADRs once a concrete driving use case exists for each - the same move ADR-0017 made for `IDomainService` and the `IAggregateRoot`-constrained repository.

## Consequences

- **Positive:** One vocabulary for storage work (`IUnitOfWork`/`IDomainRepository<T>`) instead of two competing ones.
- **Positive:** Non-transactional dispatches (pure reads) never pay for an open transaction; the common write case gets one automatically with no marker to remember.
- **Positive:** Whole-session protection is preserved for the direct-injection escape hatch - not a regression from today's guarantee.
- **Positive:** `Cancel` gives handlers a rare-case override without introducing exception-driven control flow into a library built around outcome values.
- **Positive:** Retiring `Interlocutor.EntityFrameworkCore`/`.NHibernate` removes two packages whose only remaining content would otherwise be superseded code kept around for no reason.
- **Negative:** Breaking change for every existing `ITransactionalRequest`/`TransactionBehavior` consumer - package reference changes, and `ITransactionalRequest`'s meaning changes under existing code that implements it without updating usage. No migration shim ships.
- **Negative:** The NHibernate flush-mode default is a real behavioral change for any consumer currently relying on NHibernate's auto-flush-before-query default.
- **Negative:** `Interlocutor.Persistence.EntityFrameworkCore`/`.NHibernate` pick up a new responsibility (transaction lifecycle) they didn't have under ADR-0017 - `IUnitOfWork` is no longer a pure façade, and a reader of ADR-0017 alone would no longer have the current contract.
- **Negative:** The advanced-surface member list is intentionally unresolved past the NHibernate flush-mode default - a consumer wanting, say, pessimistic locking today still has no library-provided answer beyond the documented direct-injection escape hatch.

## Alternatives considered

- **Keep `IUnitOfWork` as a pure façade (status quo, ADR-0017):** rejected - see Context.
- **`ITransactionalRequest` removed entirely, transaction always ambient:** rejected - loses the declaration-site opt-in visibility ADR-0004 valued, for the rare eager-atomicity case specifically.
- **`ITransactionalRequest`/`TransactionBehavior` kept unchanged, new model added in parallel:** rejected - two transaction-ownership mechanisms to maintain indefinitely, for marginal benefit over a clean breaking change pre-1.0.
- **Commit gate as mandatory core-dispatch behavior, not opt-in:** rejected - would amend [ADR-0009](0009-no-default-pipeline.md)'s no-default-pipeline guarantee, a materially bigger decision than this ADR set out to make.
- **Keep `Interlocutor.EntityFrameworkCore`/`.NHibernate` packages, replace their contents:** rejected - no reason to keep a package boundary stable when its only type is being replaced anyway; retiring is the more legible outcome for a future reader.
- **Automatic transaction protection scoped to `IUnitOfWork`/`IDomainRepository<T>` access only:** rejected - a regression from `TransactionBehavior`'s existing whole-session guarantee for direct-injection consumers.
- **`Cancel` as an immediate, terminal, exception-driven stop:** rejected - introduces exception-driven control flow for an expected, non-bug situation, at odds with this library's outcome-value philosophy.
- **Ship a concrete advanced-surface member list now (lock modes, stateless session, etc.):** rejected for this ADR - none of those were pressure-tested to a concrete shape in the discussion that produced this decision; deferred to focused follow-up ADRs.
