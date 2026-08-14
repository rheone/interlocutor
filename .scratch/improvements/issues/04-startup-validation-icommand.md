## Labels

ready-for-agent

## What to build

Extend `InterlocutorStartupValidation` in `Interlocutor.Abstractions` to scan for handlers of `ICommand` (non-generic, no response value) in addition to the already-scanned `IQuery<>` and `ICommand<>`.

Currently, a void command with no registered handler silently produces a no-op — the mediator sends it, the wrapper resolves no handler, and nothing happens. The startup validation should catch this just like it would for typed requests.

## Acceptance criteria

- [ ] `InterlocutorStartupValidation.ValidateHandlerRegistrations` detects an unregistered handler for `ICommand`
- [ ] Throws `InvalidOperationException` at startup with a clear message
- [ ] All existing tests still pass

## Blocked by

None — can start immediately
