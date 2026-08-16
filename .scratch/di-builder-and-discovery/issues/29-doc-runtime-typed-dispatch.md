## Labels

ready-for-agent

## What to build

A `documentation/runtime-typed-dispatch.md` covering dispatch when the request type is only known at runtime - the outbox/queue-consumer case, where a caller can't write `mediator.Send(new SomeQuery())` because it doesn't have a compile-time type to reach for. Primary source: `docs/PRD.md` §10 (Mediator, no internal handler registry) - the `docs/bootstrapreference/Conversation.md` transcript (turn 9) has useful background on the internal handler-wrapper mechanism, but it's historical design discussion, not shipped-API documentation; verify every claim against current `src/Interlocutor.Abstractions` source rather than citing that transcript as authoritative.

Cover:

- The API surface for dispatching with a runtime `Type` token (outbox replay, queue consumers deserializing a message into an unknown-until-runtime request type).
- How this differs from, and when to prefer, the normal statically-typed `Send<TResponse>(...)` call.
- Any constraints or gotchas specific to this path (e.g., what happens if the runtime type has no registered handler - does it throw, and what's the exception).

## Acceptance criteria

- [ ] Runtime-typed dispatch API is documented against the actual shipped surface, verified in `src/Interlocutor.Abstractions`
- [ ] A worked example shows an outbox or queue-consumer scenario
- [ ] Failure behavior (missing handler at runtime) is documented
- [ ] Every code example compiles against the shipped API
- [ ] Follows house conventions from ticket 21

## Blocked by

21 - Establish documentation/ directory; retire CONTEXT.md
