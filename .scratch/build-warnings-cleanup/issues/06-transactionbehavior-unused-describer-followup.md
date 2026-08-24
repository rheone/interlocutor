# 06 - TransactionBehavior accepts an IResponseOutcomeDescriber it never uses

**What to build:** Both `Interlocutor.EntityFrameworkCore.TransactionBehavior<TRequest,TResponse,TContext>` and `Interlocutor.NHibernate.TransactionBehavior<TRequest,TResponse>` accept an optional `IResponseOutcomeDescriber? describer` constructor parameter and store it in a field, mirroring `LoggingBehavior`/`AuditingBehavior`'s constructor shape - but unlike those two, neither `TransactionBehavior` ever reads the field. `LoggingBehavior` uses its own `describer` to build a human-readable outcome label for its log line; `TransactionBehavior` does no logging at all, so the field is currently dead weight that happens to still compile and construct correctly.

Found while triaging an RCS1213 ("remove unused field declaration") warning during the build-warnings-cleanup pass (ticket 03/04) - fixed there with a narrowly-scoped, explained suppression rather than a structural change, since removing the constructor parameter is a breaking API change out of scope for a warnings cleanup.

Decide and implement one of:

- Wire the field into a real diagnostic (e.g. a debug/trace-level log line describing the outcome before commit/rollback), giving the parameter an actual purpose, or
- Remove the parameter entirely from both constructors as a deliberate, documented breaking change (next major version / changelog entry), since an accepted-but-ignored parameter is worse than no parameter at all.

**Blocked by:** None - can start immediately.

**Status:** needs-triage

## Labels

ready-for-agent

## Acceptance criteria

- [ ] `describer` is either genuinely consumed by both `TransactionBehavior` implementations, or removed from both constructors
- [ ] If removed: documented as a breaking change (README/CHANGELOG, whichever this repo uses) and any call sites/tests passing an explicit `describer` argument are updated
- [ ] If wired in: the new diagnostic output doesn't duplicate `LoggingBehavior`'s existing output when both behaviors are registered together, and includes an XML doc/remarks note explaining what it adds beyond `LoggingBehavior`
- [ ] `dotnet build` and `dotnet test` pass for `Interlocutor.EntityFrameworkCore`, `Interlocutor.NHibernate`, and their test projects
