> **Status: done** - merged in `36c1416` (RegistrationProblemKind.AmbiguousClosure check + InterlocutorBuilder.Eject).

## Labels

ready-for-agent

## What to build

`IQueryHandler<,>`, `ICommandHandler<,>`, and `IVoidCommandHandler<,>` each document that the mediator expects exactly one handler per closed `<TRequest, TResponse>` pair (see `ICommandHandler`'s own remarks), but nothing enforces it. Today, scanning two different implementation types that both close the same handler contract silently registers both - nothing catches it until a container-specific, resolve-time failure the first time `Send` actually reaches `GetRequiredService<ICommandHandler<...>>()` (`CommandHandlerWrapper.Handle`). `INotificationHandler<>` is deliberately excluded from this - fan-out to many handlers per notification is its whole design (see `NotificationFanOutIntegrationTests`).

- Extend `RegistrationValidator.ComputeProblems` (checked only under `ValidateOnBuild()`, same opt-in/aggregated pattern as its existing checks) with a new `RegistrationProblemKind.AmbiguousClosure` case: for each closed service interface among the registration list's `Handler`-kind entries, if more than one distinct implementation type closes it, that's a problem. Distinct implementation type is the unit - the same implementation type appearing twice (e.g. found by both scanning and an explicit `AddHandler` call for the identical pair) is not a duplicate.
- Seed the check against the three single-winner contracts only (`IQueryHandler<,>`, `ICommandHandler<,>`, `IVoidCommandHandler<,>`); write the check itself generally (parameterized by "which open-generic contracts are single-winner") rather than hardcoding the three inline, since ticket 56 extends this same set.
- The problem's message includes full debug info: the closed service interface's name, every conflicting implementation type's name, and - when a conflicting registration came from a `AddTypeMatch` rule (ticket 51) - its `MatchedByRule` label. Mirror the existing `ConflictingLifetime` message's shape.
- Add a new builder-wide `Eject(params Type[] implementationTypes)` method on `InterlocutorBuilder`, plus a generic `Eject<TImplementation>()` overload. Callable zero or more times, additive (each call adds to one shared list, never replaces it). An ejected implementation type is removed from `Handlers` entirely - both from what the ambiguity check considers and from what actually reaches the container - not merely suppressed from the validation message.
- An explicit `AddHandler` call does **not** implicitly resolve an ambiguity among other, different implementation types still found by scanning for the same closed contract - it only overrides its own exact `(ServiceType, ImplementationType)` pair, consistent with existing precedent (`InterlocutorBuilder.Handlers`'s remarks). `Eject` remains the only way to actually clear a conflict among scanned candidates.

## Acceptance criteria

- [x] Two different implementation types closing the same `ICommandHandler<,>` (or `IQueryHandler<,>`/`IVoidCommandHandler<,>`) closed interface is reported as an `AmbiguousClosure` problem when `ValidateOnBuild()` is configured, aggregated alongside any other problems in the same `InterlocutorRegistrationValidationException`
- [x] The problem message names the closed service interface and every conflicting implementation type
- [x] Two `INotificationHandler<>` implementations for the same notification type is never reported as ambiguous
- [x] The same implementation type discovered twice for the same closed interface (e.g. scan + `AddHandler` for the identical pair) is not reported as ambiguous
- [x] `Eject(typeof(LosingHandler))` removes `LosingHandler` from `Handlers` and clears the `AmbiguousClosure` problem for its closed interface, leaving the other candidate registered
- [x] `Eject<TImplementation>()` behaves identically to the `Type`-based overload
- [x] Calling `Eject` more than once accumulates ejected types rather than replacing the list
- [x] An `AddHandler` call for one of two conflicting candidates does not, by itself, clear the `AmbiguousClosure` problem for the other candidate
- [x] Without `ValidateOnBuild()` configured, no ambiguity check runs, consistent with every other check in `RegistrationValidator`
- [x] `dotnet build` and `dotnet test` pass for the whole solution

## Blocked by

None - can start immediately
