## Labels

ready-for-agent

## What to build

With the implementation settled, bring the requirements document and architecture decision records into line with what actually shipped.

The requirements document still describes the pre-migration result-type design, the pre-rename void-command handler contract, and the old wrapper contract. New public surface introduced during this work has no requirements entry at all.

Record the decisions taken during this effort as architecture decision records, including the ones deliberately deferred, so the reasoning survives.

## Acceptance criteria

- [ ] The requirements document matches the shipped public surface, with no references to removed or renamed types
- [ ] New public surface added during this effort is documented
- [ ] An architecture decision record covers outcome classification, including the unregistered-type policy and the deferred question of whether classification should move onto the outcome types themselves
- [ ] An architecture decision record covers the value-type conversion of the case types and its default-value consequences
- [ ] An architecture decision record covers the removal of the bulk, enumerable and paged contracts
- [ ] Any package layering change made during this effort is recorded

## Blocked by

- 18-integration-outcome-matrix-and-parity
