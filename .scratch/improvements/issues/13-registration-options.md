## Labels

ready-for-agent

## What to build

Registration currently wires up handlers and the mediator and nothing else — no pipeline behaviors and no outcome describer — despite the registration method's own documentation claiming it registers behaviors. A consumer who follows the documentation and adds a behavior gets a resolve-time failure, because four behaviors inject a describer that nothing registers.

Introduce a configuration object and an overload that accepts it, so the canonical behavior order lives in the library rather than being re-derived in every consumer's startup code.

Both container integrations must expose the same surface; they must not drift.

## Acceptance criteria

- [ ] A single registration call produces a container that resolves and dispatches through the full behavior chain with no consumer-written implementations
- [ ] Behaviors register in the canonical documented order, and that order is asserted
- [ ] The outcome describer is always registered, and consumers can register their own outcome types through the same configuration object
- [ ] The default notification publish strategy and an assembly-scan filter are configurable
- [ ] Both container integrations expose an equivalent surface
- [ ] Registration documentation matches what the code actually does
- [ ] Tests are red before green

## Blocked by

- 12-default-pipeline-implementations
