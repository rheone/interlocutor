## Labels

ready-for-agent

## What to build

Extend `Interlocutor.AspNetCore` result-mapping tests beyond 2-arity `OperationOutcome<TSuccess, TFailure>` outcomes. `ResultExtensionsTests.cs`, `MinimalApiResultExtensionsTests.cs`, and `DefaultOutcomeStatusMapperTests.cs` currently only ever construct 2-arity outcomes, but CLAUDE.md's own convention - "pick the specific `OperationOutcome<TSuccess, ...>` arity/case types that operation can actually produce" - pushes real handlers toward 3-6 arity outcomes (e.g. a handler that can return `NotFound | Forbidden | ValidationErrors` together). None of that shape is exercised.

## Acceptance criteria

- [x] `ToActionResult()` is tested against outcomes of at least 3-arity and 5-arity, covering multiple distinct failure cases in the same union
- [x] `DefaultOutcomeStatusMapper` status-code selection is verified per case across a multi-arity outcome, not just the 2-arity shape
- [x] Minimal API result extensions are covered for the same multi-arity shapes
- [x] Existing 2-arity tests still pass

## Blocked by

None - can start immediately
