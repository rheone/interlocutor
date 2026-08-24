> **Status: done** - merged in `d8506d3`.

## Labels

ready-for-agent

## What to build

`documentation/transactions-and-store-integration.md`'s "Commit/Rollback Semantics" section states the TransactionBehavior decision rule in prose only, even though the rule has a genuinely non-obvious edge case that `pipeline-and-behaviors.md` already flags at `[!CAUTION]` severity: TransactionBehavior commits on a success outcome and rolls back on a `Failure` outcome - but it does **not** roll back when an outer behavior (e.g. AuthorizationBehavior) short-circuited *before TransactionBehavior ever ran*, because in that case no transaction was ever opened in the first place. Readers skimming prose can easily misread this as "any denied/failed request rolls back."

Add a flowchart that makes the three outcomes unmistakable:

1. Request never reaches TransactionBehavior (outer short-circuit) → no transaction opened, nothing to roll back.
2. Request reaches TransactionBehavior, handler returns success → commit.
3. Request reaches TransactionBehavior, handler returns a `Failure` outcome → rollback.

Branch (1) is the one doing the real work here - it's the case that's easy to miss and the reason this diagram earns its place.

**Verify by rendering** in both GitHub light and dark modes before calling this done.

## Acceptance criteria

- [ ] Flowchart shows all three branches: outer short-circuit (no transaction), success (commit), failure (rollback)
- [ ] The short-circuit-≠-rollback distinction is visually unmissable, not a minor branch label
- [ ] Diagram renders correctly via `node tools/validate-mermaid.mjs` and is confirmed legible in both GitHub light and dark rendering
- [ ] Any manual coloring uses `classDef` with explicit `fill` and `color`
- [ ] Any prose that becomes redundant with the new diagram is trimmed, not left duplicated

## Blocked by

None - can start immediately
