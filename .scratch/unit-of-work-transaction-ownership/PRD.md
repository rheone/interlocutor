# PRD: Unit of Work as Transaction Owner

Status: done - all 10 tickets implemented in c938568 ("feat: make IUnitOfWork the transaction owner (ADR-0018)"), individually verified and marked done in b5f71e4

Design decisions for this feature are recorded in [ADR-0018](../../docs/adr/0018-unit-of-work-as-transaction-owner.md). This PRD exists to break ADR-0018 into implementation issues; it does not re-derive the reasoning in the ADR.

## Problem Statement

Today, transaction ownership (`ITransactionalRequest` + `TransactionBehavior`) and the storage-access façade (`IUnitOfWork`/`IDomainRepository<T>`, from ADR-0017) are two separate mechanisms a handler author has to know about. `IUnitOfWork` deliberately does not own transaction lifecycle. That split adds a marker to remember, pins the transaction boundary to a pipeline position rather than to when a write actually happens, and leaves `Interlocutor.EntityFrameworkCore`/`Interlocutor.NHibernate` shipping a single type (`TransactionBehavior`) that a unified model would supersede.

## Solution

Make `IUnitOfWork` the transaction owner. Transactions open lazily on first write (not on dispatch start, not gated by a marker), protect the whole scoped session regardless of access path (repository, façade, or direct `Lazy<TContext>`/`Lazy<ISession>` injection), and commit/roll back automatically at pipeline completion via the existing outcome-classifier rule - plus an explicit `Cancel` for the rare manual-rollback case. `ITransactionalRequest` narrows to mean "needs an eager transaction from pipeline start for read-then-write atomicity," rather than "wants a transaction at all." `Interlocutor.EntityFrameworkCore` and `Interlocutor.NHibernate` - each shipping only `TransactionBehavior` today - are retired; their replacement ships from `Interlocutor.Persistence.EntityFrameworkCore`/`.NHibernate` instead.

Full reasoning, rejected alternatives, and what's explicitly deferred are in ADR-0018 - do not re-litigate them here; if an issue below turns up a reason to revisit one, update the ADR first.

## Out of scope

- Concrete advanced-surface members beyond the NHibernate flush-mode default (lock modes, stateless session, HQL/native SQL, `ExecuteUpdate`/`ExecuteDelete`-style bulk helpers, `Evict`/`ChangeTracker.Clear`, multi-query batching) - deferred to future, focused ADRs per ADR-0018.
- `IDomainService`, an `IAggregateRoot`-constrained repository - already out of scope per ADR-0017, unchanged here.
- A migration shim or compatibility layer for existing `ITransactionalRequest`/`TransactionBehavior` consumers - explicitly not shipped, per ADR-0018's no-shim stance.

## Issues

1. [01-baseline-contract-update.md](issues/01-baseline-contract-update.md) - `Cancel` on `IUnitOfWork`; `ITransactionalRequest`/`IExternalStoreRequest` docs narrowed.
2. [02-entityframeworkcore-transaction-behavior.md](issues/02-entityframeworkcore-transaction-behavior.md) - EF Core begin-on-first-write commit/rollback behavior.
3. [03-nhibernate-transaction-behavior.md](issues/03-nhibernate-transaction-behavior.md) - NHibernate begin-on-first-write commit/rollback behavior, plus the flush-mode default.
4. [04-entityframeworkcore-di-registration.md](issues/04-entityframeworkcore-di-registration.md) - Microsoft/Autofac registration for the new EF Core behavior.
5. [05-nhibernate-di-registration.md](issues/05-nhibernate-di-registration.md) - Microsoft/Autofac registration for the new NHibernate behavior.
6. [06-retire-entityframeworkcore-package.md](issues/06-retire-entityframeworkcore-package.md) - remove `Interlocutor.EntityFrameworkCore`.
7. [07-retire-nhibernate-package.md](issues/07-retire-nhibernate-package.md) - remove `Interlocutor.NHibernate`.
8. [08-architecture-tests.md](issues/08-architecture-tests.md) - `DependencyTests.cs` rules for the retirements and the new behaviors' home.
9. [09-samples-update.md](issues/09-samples-update.md) - update the runnable sample and the bootstrap reference doc's code.
10. [10-documentation.md](issues/10-documentation.md) - `documentation/transactions-and-store-integration.md`, glossary, and `CLAUDE.md` updates.

## Ordering

01 blocks 02 and 03. 02 blocks 04; 03 blocks 05. 04 blocks 06; 05 blocks 07. 06 and 07 block 08. 04 and 05 block 09. 08 and 09 block 10 - documentation is written last, once the shipped API, tests, and sample are stable.
