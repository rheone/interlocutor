## Labels

ready-for-agent

## What to build

`documentation/notifications-and-fan-out.md` has zero diagrams despite its entire subject being concurrency/timing — the "Publish Strategies" section prose-describes Sequential (handlers awaited one at a time; the first exception stops the remaining handlers from running) vs. Parallel (`Task.WhenAll`; every handler runs regardless of whether another one failed), which is exactly the kind of ordering/timing distinction prose alone struggles to make vivid.

Add one sequence diagram, one `INotification` publisher fanning out to multiple (e.g. three) handlers, contrasting both strategies:

- **Sequential branch**: handlers invoked one at a time in registration order; show a handler throwing partway through and the remaining handlers never being invoked.
- **Parallel branch**: use a `par`/`and` block to show all handlers invoked concurrently; show one handler failing while the others still complete, since that's the behavioral distinction that most matters to a reader choosing between the two strategies.

Place it in or near the "Which Strategy to Use" section so the diagram directly informs that choice.

**Verify by rendering** in both GitHub light and dark modes before calling this done.

## Acceptance criteria

- [ ] Diagram contrasts Sequential and Parallel publish strategies within a single `sequenceDiagram`
- [ ] Sequential branch shows a failure stopping remaining handlers from running
- [ ] Parallel branch uses `par`/`and` and shows one handler failing while others still complete
- [ ] Diagram renders correctly via `node tools/validate-mermaid.mjs` and is confirmed legible in both GitHub light and dark rendering
- [ ] Any manual coloring uses `classDef` with explicit `fill` and `color`
- [ ] Any prose that becomes redundant with the new diagram is trimmed, not left duplicated

## Blocked by

None — can start immediately
