## Labels

ready-for-agent

## What to build

Cover the full outcome-classification matrix end-to-end, and prove the two container integrations agree.

For every case type — success, not-found in both forms, forbidden, conflict in both forms, validation errors in both forms, failure in both forms, a consumer-registered custom outcome, and an unregistered custom outcome — assert both the transactional decision and the description the pipeline recorded. This is the regression net for the silent-commit defect.

Then parameterize the fixture over both container integrations so they cannot drift.

## Acceptance criteria

- [ ] Every case type is covered for commit-versus-rollback and for the recorded description
- [ ] The unregistered custom outcome follows the documented policy, asserted explicitly
- [ ] The matrix runs against both store integrations and they agree
- [ ] The matrix runs against both container integrations and they agree
- [ ] A minimal host using only the default registration dispatches through every behavior with no consumer-written implementations

## Blocked by

- 17-integration-ordering-and-short-circuit
