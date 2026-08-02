## Labels

ready-for-agent

## What to build

Existing tests are all per-type unit tests. Nothing exercises the mediator end-to-end through a real container, which is exactly where this branch's regressions live.

Create the integration test project and the shared fixture that later integration slices build on. The fixture must expose two configurations:

- **Simple** — handlers and mediator only, no behaviors, no describer, default publish strategy. The baseline that answers "does the bare library work", and the configuration most likely to be broken by registration changes.
- **Complex** — the full canonical behavior chain, pipeline stages, a consumer-registered custom outcome type, a populated request context, and test doubles for the audit sink, policy evaluator, logger and store session.

This slice covers the fixture plus basic dispatch scenarios. Ordering, transactional and container-parity scenarios follow in later slices.

## Acceptance criteria

- [ ] An integration test project exists and is registered in the solution
- [ ] The fixture exposes both configurations and is reusable by later slices
- [ ] Queries, value-returning commands and void commands each dispatch and return the handler's outcome unchanged, in both configurations
- [ ] The sender, publisher and mediator abstractions all resolve to the same scoped instance
- [ ] Resolving in two scopes yields distinct scoped handlers, and no scoped handler is captured by a longer-lived behavior

## Blocked by

- 13-registration-options
