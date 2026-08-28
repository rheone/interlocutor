> **Status: done** - merged in `36c1416` (InterlocutorBuilder.RequireUniqueClosure).

## Labels

ready-for-agent

## What to build

Ticket 55 writes the "at most one implementation per closed interface" check generally, but only ever points it at the three built-in mediator handler contracts. A consumer with their own open-generic contract that has the same single-implementation-per-closed-type expectation (for example, a per-entity export/import strategy interface) has no way to opt it into the same check or the same `Eject` escape hatch, and is left reimplementing scanning and ambiguity detection by hand.

- Add a new `InterlocutorBuilder` method (for example `RequireUniqueClosure(Type openGenericContract)`) that adds a consumer-supplied open-generic interface to the set of contracts ticket 55's `AmbiguousClosure` check considers, alongside the three built-in handler contracts.
- Every closed implementation of a registered contract found while scanning is registered the same way `HandlerDiscovery.DescribeHandler` already registers handlers - reflecting over `GetInterfaces()`, one registration per closed instantiation - not reusing `AddTypeMatch`'s predicate-based matching, since "closes this open generic" is a structural check, not a free-form predicate.
- `Eject` (ticket 55) applies uniformly - an implementation type ejected from the builder is excluded from every uniqueness-checked contract, built-in or consumer-supplied, not just handler contracts.

## Acceptance criteria

- [x] `RequireUniqueClosure(typeof(IMyOpenGenericContract<>))` causes every closed implementation of that contract found while scanning to be registered
- [x] Two implementation types closing the same instantiation of a consumer-registered contract is reported as `AmbiguousClosure` under `ValidateOnBuild()`, with the same debug info shape as ticket 55's handler-contract check
- [x] `Eject` clears a conflict on a consumer-supplied contract the same way it does for a built-in handler contract
- [x] The three built-in handler contracts from ticket 55 continue to be checked without requiring a `RequireUniqueClosure` call for them
- [x] `dotnet build` and `dotnet test` pass for the whole solution

## Blocked by

55 - Ambiguous handler validation and Eject
