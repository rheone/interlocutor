## Labels

ready-for-agent

## What to build

Cover the pipeline composition scenarios that no single implementation slice owns, using the shared integration fixture.

Behavior ordering is a documented contract with no test at all today. Assert the observed order rather than the registration order, so a regression in composition is caught rather than assumed.

## Acceptance criteria

- [ ] The observed behavior order matches the canonical documented order, with logging outermost and transaction innermost
- [ ] A failing authorization prevents the handler from running at all, and still produces a correctly shaped outcome
- [ ] A failing validation does the same
- [ ] Pipeline stage hooks pair correctly through a real container on success, on a returned failure, on a handler exception, and on a mid-chain before-hook throw
- [ ] Startup validation failures surface with a message naming the request type, for both the generic contracts and void commands

## Blocked by

- 16-integration-fixture-and-dispatch
