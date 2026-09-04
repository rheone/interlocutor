# 18 - Revisit duplicated IExternalStoreRequest check across CommitGateDriver and store suppression

**What to build:** `CommitGateDriver.Handle` checks `request is IExternalStoreRequest` to skip session resolution entirely, and EF Core's `RunWithExternalStoreSuppression` (invoked as part of the `next` delegate passed to the driver) independently re-checks the same marker to toggle `UnitOfWorkSaveChangesInterceptor.IsSuppressed`. The two checks serve different purposes (session-vs-interceptor) and are likely justified as separate today, but they are the same predicate on the same type evaluated twice across two collaborators.

This is a **watch-item, not urgent work**: with only two store packages (NHibernate, EF Core) and NHibernate having no equivalent interceptor-suppression concern, there's no immediate duplication payoff to chase. Revisit only if a third store package is added, or if EF Core's suppression logic changes in a way that makes keeping the two checks in sync harder.

**Blocked by:** None - can start immediately, but likely should stay open/deferred rather than actioned speculatively

**Status:** ready-for-agent

- [ ] Re-evaluate whether the two `IExternalStoreRequest` checks (in `CommitGateDriver` and in EF Core's suppression wrapper) can share one evaluation without entangling the session-resolution and interceptor-suppression concerns
- [ ] If a third store package is added, confirm whether it reintroduces this duplication a third time - if so, that's the trigger to act on this ticket rather than continuing to defer it
- [ ] No action needed now if the two checks remain justified as independent concerns
