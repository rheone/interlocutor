## Labels

ready-for-agent

## What to build

`documentation/pipeline-and-behaviors.md`'s "Composition Diagram" is currently a `graph TD` flowchart drawing ingress as one chain of boxes (`LoggingBehavior OnIngress → AuditingBehavior OnIngress → ... → Handler`) and egress as a second chain stacked directly below it (`Handler → TransactionBehavior OnEgress → ... → Response`). Read top to bottom, this looks like two sequential passes through the list rather than what the prose actually describes: an onion/Russian-doll model where each behavior *wraps* the next, and a single `next()` call nests all the way down to the handler and back out.

**Convert it to a `sequenceDiagram`.** One participant per behavior in recommended-chain order (Logging, Auditing, Authorization, Validation, Transaction) plus the Handler, using `activate`/`deactivate` (or the `+`/`-` arrow shorthand) so each behavior's active lifetime visibly nests inside the one before it - this is what actually communicates "wrapping" instead of "two passes."

Also add what no existing diagram shows: **the short-circuit case.** Use an `alt` block illustrating, e.g., AuthorizationBehavior denying a request - showing that when a behavior short-circuits, the remaining inner behaviors and the handler never activate, and the response flows straight back out through only the behaviors that already ran. This directly illustrates the "Filtered by Marker Interface" / short-circuit behavior the prose already describes in this file.

Apply the same dark-mode-safe styling fix as ticket 41 (`classDef` with both `fill` and `color`) if any manual coloring is kept on the new diagram.

**Verify by rendering** in both GitHub light and dark modes before calling this done.

## Acceptance criteria

- [ ] The composition diagram is a `sequenceDiagram`, not a flowchart
- [ ] Behavior nesting (the onion/wrap model) is shown via `activate`/`deactivate` or equivalent, not two stacked linear chains
- [ ] An `alt` (or equivalent) block shows a short-circuit path where inner behaviors/handler never activate
- [ ] Any manual node/participant coloring uses `classDef` with explicit `fill` and `color`
- [ ] Diagram renders correctly via `node tools/validate-mermaid.mjs` and is confirmed legible in both GitHub light and dark rendering
- [ ] Any prose that becomes redundant with the new diagram is trimmed, not left duplicated

## Blocked by

None - can start immediately
