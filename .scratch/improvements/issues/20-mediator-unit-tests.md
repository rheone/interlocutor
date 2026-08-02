> **Status: superseded** — this issue predates the common-kernel migration.
> Reason: references removed types; see remediation 06 and 16.
> Superseded by the effort in `.scratch/remediation/`. Do not pick this up.

## Labels

ready-for-agent

## What to build

Write unit tests for the `Mediator` class in `Mediance.Abstractions`. The mediator is the core dispatch mechanism — it resolves handlers from DI, chains pipeline behaviors, caches wrapper types, and routes requests. It currently has **zero unit tests**.

Test scenarios needed:

### Send(IQuery<TResponse>)
- Happy path: handler returns success Result<TResponse>
- Failure path: handler returns Failure
- Pipeline behaviors execute in correct order (outermost → innermost)
- Pipeline behavior short-circuits by not calling next()
- Pipeline behavior modifies result
- CancellationToken is forwarded to handler
- Handler is resolved fresh per call (scoped)
- Multiple behaviors compose correctly

### Send(ICommand<TResponse>)
- Same scenarios as query send

### Send(ICommand) — void command
- Happy path: handler returns Success
- Failure path: handler returns Failure
- Pipeline behaviors execute (critical — currently bypassed, see ticket 08)
- Handler is resolved per request type

### Caching
- Handler wrapper is cached in ConcurrentDictionary (verify same wrapper reused)
- Different request types get different wrappers

### Edge cases
- No handler registered throws InvalidOperationException
- Null request throws ArgumentNullException
- Cancelled token throws OperationCanceledException

Use NSubstitute for mocking IServiceProvider, IQueryHandler<,>, ICommandHandler<,>, ICommandHandler<>, and IPipelineBehavior<,>.

## Acceptance criteria

- [ ] Mediator dispatch for IQuery<TResponse> fully covered
- [ ] Mediator dispatch for ICommand<TResponse> fully covered
- [ ] Mediator dispatch for ICommand (void) covered (may be blocked by ticket 08)
- [ ] Pipeline behavior composition and ordering verified
- [ ] Caching behavior verified
- [ ] All existing tests still pass

## Blocked by

- 08-fix-void-command-dispatch (for testing void command pipeline behaviors)
