> **Amendment** - this ticket instructed citing ADR-0007 and ADR-0010 directly in the shipped `documentation/notifications-and-fan-out.md` prose. That instruction is superseded: `documentation/` files must state design rationale as fact and must never cite or link to `docs/adr/`, `docs/PRD.md`, or other `docs/` paths - see the house convention added in ticket 36. The shipped file has been corrected by ticket 36. No action needed here; historical context only.

## Labels

ready-for-agent

## What to build

A `documentation/notifications-and-fan-out.md` covering `INotification`/`IPublisher.Publish` fan-out. Primary sources: ADR-0007 (notification publish strategies, as amended), ADR-0010 (dispatch scope / fan-out), `docs/PRD.md` §13.

Cover:

- **`INotification`/`INotificationHandler<T>`**, and that a single notification can have multiple handlers.
- **Execution order**: unordered by default, with the relative-ordering attributes (run-after/run-before escape hatch) available when a consumer genuinely needs one handler to run relative to another.
- **Publish strategies**: `Sequential` (default) vs `Parallel`, what each means for handler execution.
- **Per-strategy failure semantics**, including the partial-execution caveat - what a caller can and can't assume happened if one handler throws partway through fan-out.

## Acceptance criteria

- [ ] Multiple-handlers-per-notification and unordered-by-default execution are documented
- [ ] Relative-ordering attributes are documented with an example of when to reach for them
- [ ] Both publish strategies are documented, including the failure/partial-execution semantics of each
- [ ] Every code example compiles against the shipped API
- [ ] Follows house conventions from ticket 21

## Blocked by

21 - Establish documentation/ directory; retire CONTEXT.md
