> **Status: done** - merged in `183cdbc`.

## Labels

ready-for-agent

## What to build

`documentation/runtime-typed-dispatch.md` walks through the Outbox and Inbox pattern examples in prose and code but has no diagrams, even though both patterns are notoriously easy to lose track of in prose alone - they involve multiple participants (handler, database/outbox or inbox table, background dispatcher, mediator, downstream handler) coordinating across two separate points in time (the write, and the later asynchronous processing).

Add two sequence diagrams:

- **Outbox pattern**: the originating handler writes a message to the outbox table in the same transaction as its other work → a background dispatcher later reads the outbox → calls `mediator.Send` (or the runtime-typed dispatch entry point covered earlier in this doc) with the message's runtime type → marks the outbox row dispatched.
- **Inbox pattern**: a message arrives → the inbox is checked for a duplicate (dedupe) → if not a duplicate, the message is processed → the inbox records that it was processed.

Place each near its corresponding existing example section ("Outbox Pattern Example" / "Inbox Pattern Example").

**Verify by rendering** both diagrams in both GitHub light and dark modes before calling this done.

## Acceptance criteria

- [ ] Outbox pattern diagram shows: same-transaction write → background dispatcher read → runtime-typed `mediator.Send` → marked dispatched
- [ ] Inbox pattern diagram shows: message arrival → dedupe check → process → recorded in inbox
- [ ] Both diagrams render correctly via `node tools/validate-mermaid.mjs` and are confirmed legible in both GitHub light and dark rendering
- [ ] Any manual coloring uses `classDef` with explicit `fill` and `color`
- [ ] Any prose that becomes redundant with the new diagrams is trimmed, not left duplicated

## Blocked by

None - can start immediately
