## Labels

ready-for-agent

## What to build

Extend `MissingHandlerDiagnosticTests.cs` (`tests/Interlocutor.Generator.Tests/`) to cover notification handlers and duplicate-handler registration, neither of which is exercised today - the existing 6 facts only cover missing-handler diagnostics for query, command, and void-command request types.

Notification handlers have different arity semantics than request handlers: zero-or-many registered handlers is valid for a notification, unlike a query or command, which must resolve to exactly one. The generator's diagnostics need to reflect that distinction, and a second request type with two competing concrete handlers registered should itself produce a diagnostic naming both.

## Acceptance criteria

- [ ] Zero registered `INotificationHandler<T>` implementations for a published notification does NOT produce a missing-handler diagnostic (this is valid, unlike request handlers)
- [ ] A missing-handler diagnostic is still produced for a query/command/void-command request with no handler (regression check on existing behavior)
- [ ] Two concrete handlers registered for the same query or command request produces a diagnostic naming both
- [ ] Existing `MissingHandlerDiagnosticTests.cs` assertions still pass

## Blocked by

None - can start immediately
