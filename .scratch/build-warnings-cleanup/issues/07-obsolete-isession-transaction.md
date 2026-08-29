# 07 - Replace obsolete `ISession.Transaction` reads with `GetCurrentTransaction()` in production code

**What to build:** NHibernate 5.7 (the version this repo pins) marks `ISession.Transaction` `[Obsolete("Use GetCurrentTransaction extension method instead, and check for null.")]`. Replace every *production* read of it with `NHibernate.SessionExtensions.GetCurrentTransaction(this ISession)` (confirmed via reflection against the installed 5.7.0 package: it's a static extension method already in the `NHibernate` namespace, no new `using` needed, returning a possibly-null `ITransaction`), with an explicit null check at each call site per the obsolete message's own instruction, preserving today's exact active/inactive-transaction logic:

- `src/Interlocutor.Persistence.NHibernate/NHibernateUnitOfWork.cs` (2 occurrences)
- `src/Interlocutor.Persistence.NHibernate/UnitOfWorkBehavior.cs` (3 occurrences, plus one stale `<see cref="ISession.Transaction"/>` doc-comment reference to update alongside)
- `tests/TodoTasks.Api.Tests/Persistence/DeleteUserCascadeTests.cs` (1 occurrence - TutorialProject's own persistence test, already runs against a real in-memory NHibernate session, not a mock, so this is a same-kind mechanical fix: calls `.Commit()` directly on the result)
- `tests/TodoTasks.Api.Tests/Persistence/SyncAwareCompleteTodoTaskRollbackTests.cs` (2 occurrences - same pattern as `DeleteUserCascadeTests.cs` above: a real fixture session, not a mock, one an explicit `.Transaction.Commit()` and one an `Assert.False(session.Transaction is { IsActive: true })` assertion. This file's own comment already flagged `ISession.Transaction` as obsolete and deferred to this ticket rather than picking its own replacement ad hoc)

This ticket is deliberately narrow: it doesn't touch the NSubstitute-mocked `ISession.Transaction` stubs in `tests/Interlocutor.Persistence.NHibernate.Tests/UnitOfWorkBehaviorTests.cs`, `tests/Interlocutor.Persistence.NHibernate.Tests/SharedSessionIntegrationTests.cs`, or `tests/Interlocutor.DependencyInjection.Microsoft.Tests/PipelineOrderingIntegrationTests.cs` - `GetCurrentTransaction()` is a static extension method, not mockable via NSubstitute, so those files can't follow the same fix. See ticket 08, which replaces the mocking approach itself rather than papering over it with a suppression.

**Blocked by:** None - can start immediately.

**Status:** ready-for-agent

- [ ] `dotnet build` reports zero `CS0618` warnings for `ISession.Transaction` in `src/Interlocutor.Persistence.NHibernate` or `tests/TodoTasks.Api.Tests` (including both `SyncAwareCompleteTodoTaskRollbackTests.cs` occurrences)
- [ ] Every production call site (`NHibernateUnitOfWork.cs`, `UnitOfWorkBehavior.cs`) uses `GetCurrentTransaction()` with an explicit null check, preserving the exact same active/inactive-transaction logic as today
- [ ] The stale `<see cref="ISession.Transaction"/>` doc-comment reference in `UnitOfWorkBehavior.cs` is updated to reference `GetCurrentTransaction()` instead
- [ ] `dotnet test` passes for `Interlocutor.Persistence.NHibernate.Tests` and `TodoTasks.Api.Tests` with no behavior change (the mocked tests in `Interlocutor.Persistence.NHibernate.Tests` still pass as-is, still stubbing the now-more-obsolete `ISession.Transaction` - ticket 08 replaces them, not this one), and the full solution `dotnet test` remains green
