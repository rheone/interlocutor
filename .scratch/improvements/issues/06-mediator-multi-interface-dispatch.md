> **Status: folded in** — absorbed into
> `.scratch/improvements/issues/23-collapse-dispatch-wrapper-hierarchy.md`, which rewrites the same
> cache-lookup code this issue targets. Do not pick this up separately; pick up 23 instead.

> **Status: folded in** — absorbed into
> `.scratch/improvements/issues/23-collapse-dispatch-wrapper-hierarchy.md`, which rewrites the same
> cache-lookup code this issue targets. Do not pick this up separately; pick up 23 instead.

## Labels

ready-for-agent

## What to build

The mediator caches handler wrappers to avoid reflection on every dispatch. The query and command caches are keyed by request type alone, then cast to a response-typed wrapper.

A request type implementing more than one request interface — for example a query declared with two different response types — therefore returns the wrapper built for whichever response type was dispatched first, and the cast fails on the second dispatch.

The void-command cache on the same class already keys by both request and result type. Apply that same approach to the query and command caches.

## Acceptance criteria

- [ ] A request implementing two request interfaces with different response types dispatches successfully for both, in either order
- [ ] Repeat dispatches still hit the cache rather than re-reflecting
- [ ] Behavior is correct across multiple container scopes
- [ ] Tests observe this through the public sender interface only; the cache itself stays an untested internal
- [ ] Tests are red before green

## Blocked by

None — can start immediately
