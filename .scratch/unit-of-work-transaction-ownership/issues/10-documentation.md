# 10 - Documentation

**What to build:** A reader following `documentation/` and `CLAUDE.md` sees the current, shipped transaction model - no stale references to `TransactionBehavior`'s old contract, `ITransactionalRequest`'s old meaning, or the retired packages.

**Blocked by:** 08, 09.

**Status:** ready-for-agent

- [ ] `documentation/transactions-and-store-integration.md` rewritten for the new model
- [ ] `documentation/glossary.md`'s "Transaction scope" and "Lazy resolution" entries updated
- [ ] `CLAUDE.md`'s package-layering table updated to reflect the retired/renamed packages
- [ ] Every code example in the updated docs compiles against the shipped API (house style truthfulness rule)
