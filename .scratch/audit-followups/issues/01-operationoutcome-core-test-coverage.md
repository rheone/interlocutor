## Labels

ready-for-agent

## What to build

Restore real test coverage for `OperationOutcome<...>`, the library's central discriminated-union result type. The only existing test file for it, `tests/Interlocutor.CommonKernel.Tests/OperationOutcomeTests.cs`, is entirely commented out and references a stale API (`OperationOutcome<int>`, `NotFound("Entity", 1)`) that wouldn't compile against the current type. None of the five declared arities (`src/Interlocutor.CommonKernel/OperationOutcome.cs`) has any active test today.

A developer picking `OperationOutcome<TSuccess, TFailure1>` through `OperationOutcome<TSuccess, TFailure1, ..., TFailure5>` for a handler should be able to trust that construction from each case, implicit conversion from each case type, and exhaustive `switch`/pattern matching over the union all behave correctly - for every arity, not just the smallest one.

## Acceptance criteria

- [ ] Each of the 5 declared arities has tests covering construction from every case type it declares
- [ ] Implicit conversion from a bare case value (e.g. `Success`, `NotFound`) to the union type is covered for each arity
- [ ] Exhaustive `switch`/pattern-matching over the union correctly discriminates every case, for each arity
- [ ] The rewritten test file compiles and passes against the current `OperationOutcome` API - no stale signatures
- [ ] All existing tests still pass

## Blocked by

None - can start immediately
