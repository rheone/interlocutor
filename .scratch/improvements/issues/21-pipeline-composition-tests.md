> **Status: superseded** — this issue predates the common-kernel migration.
> Reason: its blocker (`20-mediator-unit-tests`) is itself superseded; the same ground is covered by
> `.scratch/improvements/issues/16-integration-fixture-and-dispatch.md` and
> `.scratch/improvements/issues/17-integration-ordering-and-short-circuit.md`.
> Superseded by the effort in `.scratch/remediation/`. Do not pick this up.

## Labels

ready-for-agent

## What to build

Write integration tests that verify multiple `IPipelineBehavior<TRequest,TResponse>` instances compose correctly in a real DI container with a real `Mediator`.

These tests should use a lightweight DI setup (Microsoft.Extensions.DependencyInjection) to register:
- A dummy handler (query or command)
- Multiple pipeline behaviors (e.g., ValidationBehavior + TransactionBehavior)
- The Mediator

Then send a request through and verify:
- All behaviors execute in registered order
- A behavior that short-circuits (returns Failure) stops subsequent behaviors
- Behaviors receive and can modify responses
- The handler is only invoked once
- The DI scope is respected (scoped services resolved per send)

This catches composition bugs that unit tests of individual behaviors or the mediator alone might miss.

Consider using a test helper that records behavior execution order (e.g., a `RecordingBehavior` with `ConcurrentQueue<string>`).

## Acceptance criteria

- [ ] Test with 2+ behaviors verifies registration order execution
- [ ] Test with short-circuit behavior verifies early exit
- [ ] Test verifies scoped handler resolution
- [ ] All existing tests still pass

## Blocked by

- 20-mediator-unit-tests (establishes the test infrastructure)
