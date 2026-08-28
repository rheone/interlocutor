# PRD: Optional Persistence Abstraction - Domain Repository and Unit of Work

Status: done - all 6 issues closed (implemented)

Design decisions for this feature are recorded in [ADR-0017](../../docs/adr/0017-optional-persistence-abstraction-repository-and-unit-of-work.md). Background research is in [research.md](research.md). This PRD exists to break ADR-0017 into implementation issues; it does not re-derive the reasoning in the ADR.

## Problem Statement

Handlers that need storage today inject `Lazy<TContext>` (EF Core) or `Lazy<ISession>` (NHibernate) directly. That path works and stays fully supported. Consumers who want a narrower, store-agnostic CRUD/paging surface - for testability, or to avoid hand-rolling the same `GetById`/paging query shape in every handler - have no library-provided option.

## Solution

An optional, opt-in persistence-abstraction layer: `Interlocutor.Persistence.Abstractions` (container/ORM-neutral interfaces) plus `Interlocutor.Persistence.EntityFrameworkCore` and `Interlocutor.Persistence.NHibernate` (implementations), mirroring the existing `Interlocutor.DependencyInjection.{Abstractions,Autofac,Microsoft}` package split. `IUnitOfWork` is a façade over the same scoped `DbContext`/`ISession` that `TransactionBehavior` already manages - it does not own transaction begin/commit/rollback. `IDomainRepository<TEntity, TKey>` is generic, unconstrained, and exposes CRUD, get-many, get-paged (with expression-based filter and multi-column sort), and an `IQueryable<TEntity>` escape hatch.

Full type shapes, naming, and rejected alternatives are in ADR-0017 - do not re-litigate them here; if an issue below turns up a reason to revisit one, update the ADR first.

## Out of scope

- `IDomainService` - no concrete shape agreed; deferred to a future ADR.
- An `IAggregateRoot`-constrained repository variant - noted as likely future work in ADR-0017, not built now.
- Any change to `TransactionBehavior`'s public contract, `ITransactionalRequest`/`IExternalStoreRequest` semantics, or the direct-`DbContext`/`ISession`-injection path - all remain unchanged.
- AOT compatibility beyond the existing trim-compatible baseline (ADR-0016).

## Issues

1. [01-persistence-abstractions-package.md](issues/01-persistence-abstractions-package.md) - `Interlocutor.Persistence.Abstractions` package, `IUnitOfWork`, `IDomainRepository<TEntity,TKey>`, paging/sort types.
2. [02-entityframeworkcore-implementation.md](issues/02-entityframeworkcore-implementation.md) - `Interlocutor.Persistence.EntityFrameworkCore`.
3. [03-nhibernate-implementation.md](issues/03-nhibernate-implementation.md) - `Interlocutor.Persistence.NHibernate`.
4. [04-dependency-injection-registration.md](issues/04-dependency-injection-registration.md) - scoped registration wiring for both DI packages.
5. [05-architecture-tests.md](issues/05-architecture-tests.md) - `DependencyTests.cs` layering rules for the new packages.
6. [06-documentation.md](issues/06-documentation.md) - new `documentation/` reference doc plus the `CLAUDE.md`/glossary pagination coordination note.

## Ordering

01 blocks 02, 03, 04, 05. 02 and 03 block 04. 04 and 05 can run in parallel with each other once their own blockers clear. 06 is best done last, once the shipped API is stable, but has no hard technical blocker.
