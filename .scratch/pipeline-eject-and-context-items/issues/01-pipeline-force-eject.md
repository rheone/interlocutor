> **Status: done** - implemented alongside Ticket 02 in the same session; see `PRD.md` for the full background.

## Labels

ready-for-agent

## What to build

A developer, running normally with no cancellation involved, needs to abandon a pipeline dispatch or a handler from anywhere in its own call graph - including several private-method calls deep inside a handler body - and force the dispatch to end immediately with a legal outcome case they choose themselves. No further pipeline steps run, the handler doesn't run if the eject happens in an earlier behavior, and no transaction commits.

This is explicitly **not** cooperative cancellation: cancellation reacts to an ambient `CancellationToken` the caller controls; this is a developer's own code deciding, mid-flight, that the operation is at a dead end. The new mechanism must be named and documented so it is never confused with `CancellationBehavior`/`Cancelled`.

A call to force-eject must be recoverable at the nearest enclosing pipeline-composition call site, so every behavior that sits further out in the onion (auditing, the transaction commit/rollback gate, logging) sees an ordinary returned response - never a thrown exception - exactly as if the ejecting code had returned that response directly. This has to hold across every place the library composes a behavior chain around a handler call, including the source generator's compile-time dispatch path, not only the three hand-written wrappers - a consumer using generated dispatch must get the same capability as one using the hand-written wrappers.

No new transaction-gating logic is needed: the existing unit-of-work commit/rollback gate already decides commit-vs-rollback from the outcome's classification, not from how that outcome was produced, so an ejected non-success outcome is rolled back exactly like one returned normally.

## Acceptance criteria

- [ ] A handler can force-eject from a nested private method, several calls deep, and the mediator dispatch returns the chosen outcome normally - no exception reaches the caller
- [ ] A pipeline behavior can force-eject before calling `next()`, skipping the handler and every later behavior entirely
- [ ] A pipeline behavior can force-eject from its own logic after calling `next()`, and any behavior further out still runs its own post-`next()` logic against the ejected outcome as if it were an ordinary return value
- [ ] The mechanism works identically whether a request dispatches through the hand-written command/query wrappers or through the source generator's compile-time dispatch path
- [ ] An exception unrelated to this mechanism still propagates unchanged - the catch added to support this is narrowly scoped, not a blanket swallow
- [ ] A non-success ejected outcome is rolled back by the existing unit-of-work transaction gate with no changes to that gate's own logic
- [ ] The reference pipeline-behaviors documentation gains a section introducing the mechanism, explicitly distinguishing it from cooperative cancellation, with a worked example
- [ ] The glossary gains entries for the new public types, placed away from the cancellation-related entries

## Blocked by

None - can start immediately.
