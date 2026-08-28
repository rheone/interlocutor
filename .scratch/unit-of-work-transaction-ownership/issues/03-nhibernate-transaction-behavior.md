# 03 - NHibernate: unit-of-work-owned transaction behavior

**What to build:** The same guarantee as the EF Core behavior (see ticket 02), implemented for NHibernate's `ISession`: transaction opens on first write regardless of access path, commits/rolls back per the outcome classifier or an explicit `Cancel`, and `ITransactionalRequest` forces an eager begin. Because NHibernate's `ISession` can auto-flush pending changes before a query runs by default, the session's flush mode must default to a non-auto mode so nothing writes to the database ahead of the transaction this behavior opens - without this, whole-session protection (ticket's core guarantee) does not hold for NHibernate.

**Blocked by:** 01.

**Status:** ready-for-agent

- [ ] New behavior, registered opt-in, applies to every request once registered - no marker needed to participate
- [ ] Transaction opens on first write regardless of whether the write came through `IDomainRepository`, `IUnitOfWork`, or direct `Lazy<ISession>` access
- [ ] Session flush mode defaults so no auto-flush occurs before the transaction opens
- [ ] A dispatch that never writes never opens a transaction
- [ ] Commit on classifier-reported success; rollback on classifier-reported failure
- [ ] Calling `IUnitOfWork.Cancel` rolls back immediately; the handler's own returned outcome is preserved
- [ ] A request implementing `ITransactionalRequest` gets an eager transaction begin at pipeline start rather than on first write
- [ ] A request implementing `IExternalStoreRequest` never gets a local transaction, even if it touches the scoped `ISession`
