> **Status: done** - merged in `b232fdd`.

## Labels

ready-for-agent

## What to build

One fluent builder that is the only way to configure Interlocutor, added alongside the legacy overloads so nothing breaks yet. This is the expand half of an expand–contract; ticket 04 does the contract.

A developer configures assembly scanning, defaults, overrides, behaviors, and stages through a single chained call that reads top to bottom as the execution order. The same builder surface works identically on both containers.

Behavior this establishes:

- **Nothing runs unless asked for.** A bare configuration call registers the mediator, discovered handlers, and stages - and zero pipeline behaviors. A developer who wants logging asks for logging.
- **A behavior's dependencies are registered only when that behavior is.** No orphan audit sink, policy evaluator, or context accessor for a pipeline that does not use them.
- **A consumer's own registration of any library default wins regardless of call order.** Defaults use try-add semantics, so registering before the library call wins; explicit override methods on the builder cover registering after it.
- **Behaviors form a flat ordered list in builder-call order.** A given request's chain is that list filtered to the entries that apply, preserving position. No profiles, no numeric ordering, no merge rules.
- **Registering the same behavior more than once is legal** and runs it more than once, at each position it was added.
- **An opt-in method applies the recommended chain** - logging, auditing, authorization, validation, transaction - for developers who want a sensible pipeline without composing it. It is sugar, never a default.

The three configuration properties that are currently declared but never read - default publish strategy, outcome describer type, and startup validation - must either take effect through the builder or not exist on it.

## Acceptance criteria

- [ ] A single fluent builder configures scanning, defaults, overrides, behaviors, and stages
- [ ] The builder surface is identical across both containers, driven from the shared registration core
- [ ] A bare configuration call registers zero pipeline behaviors
- [ ] A behavior's dependencies are registered only alongside that behavior
- [ ] A consumer registration of any default wins whether it is made before or after the library call
- [ ] Behavior execution order matches builder-call order, asserted by observed order rather than registration order
- [ ] The same behavior registered twice executes twice
- [ ] An opt-in method applies the recommended chain in the documented order
- [ ] Every option exposed on the builder demonstrably takes effect
- [ ] Legacy registration overloads still work and their tests still pass

## Blocked by

02 - Container-neutral registration core
