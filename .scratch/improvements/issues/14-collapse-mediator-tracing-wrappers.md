# 14 - Collapse Mediator's tracing wrappers and the notification-wrapper shared preamble

> **Status: done** - merged in `863fb5e` (implement code-review and architecture-review follow-up tickets). `Mediator.cs` now has a single generic `TraceAsync<TResult>` helper used by send/dispatch/publish; `NotificationHandlerWrapper` carries the shared cast/resolve/tag preamble via `ResolveHandlers<TNotification>`, consumed by both `SequentialNotificationHandlerWrapper` and `ParallelNotificationHandlerWrapper`.

**What to build:** `Mediator.cs`'s three near-identical tracing methods (`TraceSendAsync`, `TraceDispatchAsync`, `TracePublishAsync` - each repeating the same start-Activity/tag-request-type/try-dispatch/tag-outcome/catch-SetStatus-rethrow skeleton) collapse into one generic tracing helper, parameterized over whether the dispatch produces a taggable `IOperationOutcome`. Separately, `SequentialNotificationHandlerWrapper` and `ParallelNotificationHandlerWrapper`'s duplicated 3-line preamble (cast notification, resolve handlers, tag activity with handler types) moves onto the shared `NotificationHandlerWrapper` base class, which today declares only the abstract `Handle` signature.

**Blocked by:** None - can start immediately

- [x] `Mediator.cs` has one tracing helper used by all three call sites (send, dispatch, publish); behavior (tags set, exception handling, `Activity.SetStatus` on error) is unchanged for each
- [x] `NotificationHandlerWrapper` base class carries the shared cast/resolve/tag preamble; `SequentialNotificationHandlerWrapper` and `ParallelNotificationHandlerWrapper` only implement their diverging dispatch loop (sequential vs. `WhenAll`)
- [x] All existing `Interlocutor.Abstractions.Tests` covering tracing tags, exception propagation, and both notification publish strategies pass unchanged
- [x] No public API changes - this is internal-only deduplication
