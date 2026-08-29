# 10 - Documentation

**What to build:** A reader following `documentation/` and `CLAUDE.md` sees the current, shipped transaction model - no stale references to `TransactionBehavior`'s old contract, `ITransactionalRequest`'s old meaning, or the retired packages.

**Blocked by:** 08, 09.

**Status:** done

- [x] `documentation/transactions-and-store-integration.md` rewritten for the new model
- [x] `documentation/glossary.md`'s "Transaction scope" and "Lazy resolution" entries updated
- [x] `CLAUDE.md`'s package-layering table updated to reflect the retired/renamed packages
- [x] Every code example in the updated docs compiles against the shipped API (house style truthfulness rule)

## Comments

Already implemented, likely alongside commit `c938568` or shortly after - verified against the current tree: `documentation/transactions-and-store-integration.md` describes the current begin-on-first-write/`UnitOfWorkBehavior`/narrowed-`ITransactionalRequest` model with no stale `TransactionBehavior` contract references; `documentation/glossary.md`'s "Transaction scope" and "Lazy resolution" entries both describe the current model; `CLAUDE.md`'s package-layering table describes `Interlocutor.Persistence.EntityFrameworkCore`/`.NHibernate` as shipping the commit-gate `UnitOfWorkBehavior` that supersedes the retired packages. Every code example checked compiles against the shipped API.
