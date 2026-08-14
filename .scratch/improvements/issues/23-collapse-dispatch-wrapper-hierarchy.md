## Labels

ready-for-agent

## What to build

Collapse the triplicated dispatch wrapper hierarchy in `Interlocutor.Abstractions` into one deep module.

Today, `QueryHandlerWrapper<TQuery,TResponse>`, `CommandHandlerWrapper<TCommand,TResponse>`, and
`VoidCommandHandlerWrapper<TCommand,TResult>` each carry a byte-for-byte identical `Handle()` body —
resolve the typed handler, seed the innermost pipeline delegate, then loop
`services.GetServices<IPipelineBehavior<TRequest,TResponse>>().Reverse()` composing the onion chain
(ADR-0002) — differing only in which DI handler interface they resolve
(`IQueryHandler<,>` / `ICommandHandler<,>` / `IVoidCommandHandler<,>`). They also sit under two
near-identical base classes, `HandlerWrapper<TResponse>` and `VoidHandlerWrapper<TResult>` (the latter
constrained to `IOperationOutcome` for no reason the base class body actually uses). `Mediator.cs` mirrors
the fragmentation with three separate `GetOrAdd` + `Activator.CreateInstance(...MakeGenericType(...))`
blocks, one per cache.

Target shape:

1. Merge `HandlerWrapper<TResponse>` and `VoidHandlerWrapper<TResult>` into one
   `RequestHandlerWrapper<TRequest,TResponse>` (drop the `IOperationOutcome` constraint).
2. The merged base owns the chain-composition loop entirely. It exposes one abstract method — resolve the
   innermost handler delegate for the concrete request — and calls it once from `Handle()`, which then
   builds and runs the behavior chain exactly once (not three times).
3. `QueryHandlerWrapper`, `CommandHandlerWrapper`, `VoidCommandHandlerWrapper` shrink to ~10-line overrides
   of that one method — the only thing that actually varies across them.
4. In `Mediator.cs`, extract one shared private helper for the `GetOrAdd` +
   `Activator.CreateInstance(...MakeGenericType(...))` boilerplate, called once per `Send` overload. Keep
   three separate `ConcurrentDictionary` cache fields — the key shapes are a real requirement, not
   incidental duplication (see point 5).
5. Fold in the fix from `.scratch/remediation/issues/06-mediator-multi-interface-dispatch.md`: key the
   query and command caches by `(RequestType, ResponseType)`, the same tuple shape the void-command cache
   already uses, instead of request type alone. A request type implementing more than one request
   interface with different response types currently collides in the cache and the second dispatch's cast
   fails. Since this refactor is rewriting the exact cache-lookup code that bug lives in, fix it here
   rather than reintroducing the same bug in newly-written code and fixing it again later. All three
   caches end up with a uniform `(Type, Type)` key shape as a consequence — that's a side effect of the
   fix, not a goal in itself.
6. Rewrite `tests/Interlocutor.Abstractions.Tests/MediatorTests.cs`'s `Send_Query_CachesWrapper`, which
   currently ends in `Assert.True(true)` and verifies nothing. Replace with a real signal — e.g. object
   reference equality of the resolved wrapper across repeated dispatches of the same request type, or a
   call-count spy — since this refactor touches the exact caching mechanism that test claims to cover.

Scope explicitly excludes the notification wrapper pair (`SequentialNotificationHandlerWrapper` /
`ParallelNotificationHandlerWrapper`) — same duplication shape, but a different variation axis (fan-out to
N handlers, not behavior-chain composition around one handler), so it doesn't belong under the same merged
base.

All affected types are `internal` — no public API or semver impact.

## Acceptance criteria

- [ ] `HandlerWrapper<TResponse>` and `VoidHandlerWrapper<TResult>` are merged into one
      `RequestHandlerWrapper<TRequest,TResponse>`
- [ ] `QueryHandlerWrapper`, `CommandHandlerWrapper`, `VoidCommandHandlerWrapper` each override exactly one
      method; the reverse-registration-order behavior-chain loop exists in exactly one place
- [ ] `Mediator.cs`'s three `GetOrAdd` + `Activator.CreateInstance(...MakeGenericType(...))` blocks are
      replaced by calls to one shared private helper
- [ ] Query and command caches key by `(RequestType, ResponseType)`; a request type implementing two
      request interfaces with different response types dispatches successfully for both, in either order
- [ ] Repeat dispatches of the same request type still hit the cache rather than re-reflecting
- [ ] `Send_Query_CachesWrapper` (and its command/void-command equivalents if present) assert real caching
      behavior, not `Assert.True(true)`
- [ ] All existing tests still pass

## Blocked by

None — can start immediately. Supersedes/absorbs
`.scratch/remediation/issues/06-mediator-multi-interface-dispatch.md` (see point 5 above) — mark that
issue as folded in here rather than picking it up separately.
