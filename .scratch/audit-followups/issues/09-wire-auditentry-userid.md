## Labels

ready-for-agent

## What to build

Wire `AuditEntry.UserId` from the ambient request context instead of always emitting `null`. `AuditingBehavior.Handle` (`src/Interlocutor.Abstractions/Pipeline/AuditingBehavior.cs`) currently constructs `new AuditEntry(request.ActionName, typeof(TRequest).Name, outcome, DateTime.UtcNow)` with no user identity, even though `AuditEntry.UserId`'s own XML doc describes it as "the authenticated user ID from request context." This was a proof-of-concept gap deliberately deferred by the remediation effort (`.scratch/remediation/discovery.md`); this ticket closes it.

Inject `IRequestContextAccessor` into `AuditingBehavior` and populate `UserId` from it when a value is available, leaving it `null` when the accessor reports none (e.g. no ambient HTTP context, or an anonymous request) rather than throwing.

## Acceptance criteria

- [ ] `AuditingBehavior` accepts an `IRequestContextAccessor` dependency
- [ ] When the accessor reports a current user, `AuditEntry.UserId` is populated with it end-to-end through a real `Send`
- [ ] When the accessor reports no current user (or none is registered), `UserId` remains `null` and auditing still succeeds - no exception
- [ ] `AspNetCoreRequestContextAccessor` (or an equivalent test double) is used in at least one test to prove the wiring works through the real accessor, not just a mock
- [ ] Existing `AuditingBehaviorTests.cs` assertions still pass

## Blocked by

None - can start immediately
