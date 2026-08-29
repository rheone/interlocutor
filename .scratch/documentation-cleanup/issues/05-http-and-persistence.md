> **Status: done**

## Labels

ready-for-agent

## What to build

Cleanup pass over `documentation/aspnetcore-integration.md`, `documentation/transactions-and-store-integration.md`, `documentation/persistence-abstraction.md`, `documentation/persistence-implementations.md` following the standing decisions in `../PRD.md`. `aspnetcore-integration.md` remains the only file permitted to mention HTTP status codes - confirm the other three don't. `transactions-and-store-integration.md` already correctly states `ITransactionalRequest` "no longer means what it used to" as a factual clarification of current behavior, not stale language to remove - keep that framing but re-check the wording reads as current-state, not history. Verify persistence docs against `src/Interlocutor.Persistence.Abstractions`, `.EntityFrameworkCore`, `.NHibernate`.

## Acceptance criteria

- [x] HTTP status codes appear only in `aspnetcore-integration.md`
- [x] No stale language remains
- [x] Every claim verified against current `src/` persistence code
- [x] Glossary-first-mention links added where appropriate

## Findings beyond the original scope

`aspnetcore-integration.md` was the most drifted file found so far - large parts described a mapper API and status-code table that no longer match `Interlocutor.AspNetCore`:

- The whole "Custom HTTP Mapping" section implemented a nonexistent `IOperationOutcomeMapper` interface and read `outcome.IsSuccess`/`outcome.Failure` off a bare `IOperationOutcome` (same false-marker-interface premise as unit 04's finding). The real type is `IOutcomeStatusMapper.Map<TFailure>(failure)` - invoked only for the failure case, returning an `OutcomeHttpMapping` (status + optional body). Rewrote the section around subclassing `DefaultOutcomeStatusMapper` or implementing `IOutcomeStatusMapper` directly.
- The status-code table was wrong in two load-bearing ways the source itself calls out as deliberate: `ValidationErrors` maps to **422**, not 400, with a plain `ProblemDetails` (no per-field `errors` dictionary - `ValidationError` doesn't carry a field name); and the generic `Failure` case maps to **400**, not 500 - the source's own doc comment explains a prior version of the mapper used 500 and that was deliberately changed, because `Failure` is an outcome a handler chose to report, not a server fault. The doc still described the old 500 behavior. Also added the previously-undocumented `Cancelled`/`Timeout`/`RateLimited`/`Failures`/unrecognized-type rows.
- "Minimal API" reused the controller-oriented `ToActionResult()` (`IActionResult`); the library ships a parallel `ToMinimalApiResult()` (`IResult`) specifically for this, never mentioned anywhere in the file.
- The unit-command success response is **204 No Content**, not 200 OK as the table claimed.
- Both `[Fact]` test examples wouldn't compile: one mocked `Send(IQuery<IOperationOutcome>, ...)` for a call that's actually a command, and constructed `new OperationOutcome<int>(42)` - a single-type-argument arity that doesn't exist (`OperationOutcome<TSuccess, ...>` is 2-6 arities only, confirmed against `OperationOutcome.cs`). The other asserted `Assert.IsType<NotFoundResult>(result)`, but a `null`-body `OutcomeHttpMapping` renders as a plain `StatusCodeResult`, which `IsType` (exact-type) rejects. Fixed both.
- `transactions-and-store-integration.md` had the same invalid-arity bug three separate times (`OperationOutcome<Success>`, `OperationOutcome<int>` twice) - a single generic type argument isn't a valid arity. Fixed each to a real arity (`OperationOutcome`, `OperationOutcome<int, InsufficientStock>`, `OperationOutcome<int, Failure>`), and fixed a `.Failure` reference on a 2-arity outcome (real member is `.Value`, cast to the known failure type - `.Failure` doesn't exist anywhere in the API).
- `persistence-abstraction.md` and `persistence-implementations.md` were verified thoroughly against `src/Interlocutor.Persistence.*` and found accurate as written - no changes needed beyond confirming.
