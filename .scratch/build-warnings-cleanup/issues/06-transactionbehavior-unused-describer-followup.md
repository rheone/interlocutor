# 06 - TransactionBehavior accepts an IResponseOutcomeDescriber it never uses

**What to build:** Both `Interlocutor.EntityFrameworkCore.TransactionBehavior<TRequest,TResponse,TContext>` and `Interlocutor.NHibernate.TransactionBehavior<TRequest,TResponse>` accept an optional `IResponseOutcomeDescriber? describer` constructor parameter and store it in a field, mirroring `LoggingBehavior`/`AuditingBehavior`'s constructor shape - but unlike those two, neither `TransactionBehavior` ever reads the field. `LoggingBehavior` uses its own `describer` to build a human-readable outcome label for its log line; `TransactionBehavior` does no logging at all, so the field is currently dead weight that happens to still compile and construct correctly.

Found while triaging an RCS1213 ("remove unused field declaration") warning during the build-warnings-cleanup pass (ticket 03/04) - fixed there with a narrowly-scoped, explained suppression rather than a structural change, since removing the constructor parameter is a breaking API change out of scope for a warnings cleanup.

Decide and implement one of:

- Wire the field into a real diagnostic (e.g. a debug/trace-level log line describing the outcome before commit/rollback), giving the parameter an actual purpose, or
- Remove the parameter entirely from both constructors as a deliberate, documented breaking change (next major version / changelog entry), since an accepted-but-ignored parameter is worse than no parameter at all.

**Blocked by:** None - can start immediately.

**Status:** closed

## Labels

ready-for-agent

## Acceptance criteria

- [x] `describer` is either genuinely consumed by both `TransactionBehavior` implementations, or removed from both constructors
- [ ] If removed: documented as a breaking change (README/CHANGELOG, whichever this repo uses) and any call sites/tests passing an explicit `describer` argument are updated
- [x] If wired in: the new diagnostic output doesn't duplicate `LoggingBehavior`'s existing output when both behaviors are registered together, and includes an XML doc/remarks note explaining what it adds beyond `LoggingBehavior`
- [x] `dotnet build` and `dotnet test` pass for `Interlocutor.EntityFrameworkCore`, `Interlocutor.NHibernate`, and their test projects

## Comments

Wired in (not removed): both `TransactionBehavior` implementations now accept an optional `ILogger<TransactionBehavior<...>>` (defaulting to `NullLogger<T>.Instance`), and use the previously-unused `describer` field to emit one `LogLevel.Debug` line per invocation naming the commit/rollback decision. XML remarks on both types explain why this doesn't duplicate `LoggingBehavior`'s `Information`-level line. New constructor parameter is optional and appended last, so no existing call site needed updating - not a breaking change, so the "if removed" criterion doesn't apply. Also renamed the NHibernate field from `_describer` to `describer` for naming consistency with the EF Core sibling (unrequested but low-risk drive-by fix, no other call sites reference it).

Covered by new tests in both `TransactionBehaviorCommitRollbackTests` (EF Core) and `TransactionBehaviorTests` (NHibernate) using a hand-rolled `ILogger<T>` fake (NSubstitute can't proxy a closed generic over a private nested test type without an `InternalsVisibleTo` grant). Two-axis `/code-review` (Standards + Spec) ran against the diff and flagged the initial tests as only asserting "one Debug line fired" without checking message content, which wouldn't have caught a commit/rollback wording swap - tests were strengthened to assert the actual message text (and, doing so, caught a real test bug: the NHibernate commit-line test's `"done"` string isn't one of the kernel's registered Success/Failure case types, so the *default* classifier fail-closes it as a failure per `OutcomeClassifierBuilder`'s documented policy - fixed by passing an explicit success-reporting stub classifier, matching the existing sibling test's pattern). Full suite: 763/763 passing.
