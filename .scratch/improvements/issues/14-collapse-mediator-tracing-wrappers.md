# 14 - Collapse Mediator's tracing wrappers and the notification-wrapper shared preamble

**What to build:** `Mediator.cs`'s three near-identical tracing methods (`TraceSendAsync`, `TraceDispatchAsync`, `TracePublishAsync` - each repeating the same start-Activity/tag-request-type/try-dispatch/tag-outcome/catch-SetStatus-rethrow skeleton) collapse into one generic tracing helper, parameterized over whether the dispatch produces a taggable `IOperationOutcome`. Separately, `SequentialNotificationHandlerWrapper` and `ParallelNotificationHandlerWrapper`'s duplicated 3-line preamble (cast notification, resolve handlers, tag activity with handler types) moves onto the shared `NotificationHandlerWrapper` base class, which today declares only the abstract `Handle` signature.

**Blocked by:** None - can start immediately

- [ ] `Mediator.cs` has one tracing helper used by all three call sites (send, dispatch, publish); behavior (tags set, exception handling, `Activity.SetStatus` on error) is unchanged for each
- [ ] `NotificationHandlerWrapper` base class carries the shared cast/resolve/tag preamble; `SequentialNotificationHandlerWrapper` and `ParallelNotificationHandlerWrapper` only implement their diverging dispatch loop (sequential vs. `WhenAll`)
- [ ] All existing `Interlocutor.Abstractions.Tests` covering tracing tags, exception propagation, and both notification publish strategies pass unchanged
- [ ] No public API changes - this is internal-only deduplication
