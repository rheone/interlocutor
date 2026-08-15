## Labels

ready-for-agent

## What to build

Document what this effort changed, so a developer arriving at the library understands the composition model rather than discovering it by experiment.

Cover, from the developer's perspective:

- **The builder** as the single configuration entry point, with the migration note that every older overload is gone.
- **The no-default-pipeline model** — nothing runs unless asked for, behaviors form a flat ordered list in call order, a request's chain is that list filtered by generic constraints on the marker interfaces, and the same behavior may appear more than once. Include the recommended chain *and its rationale* — why auditing sits outside authorization, why transaction is innermost — because a developer composing their own chain needs the reasoning, not just the sequence.
- **Handler selection**: generic handlers serving a family, closed beats open, ambiguity as a build error, the override attribute, and the explicit guarantee that namespace and assembly location never affect dispatch.
- **Pipeline stages**, including short-circuiting and the unwind guarantees.
- **Notification fan-out**, unordered execution with relative ordering attributes, and the per-strategy failure semantics including the partial-execution caveat.
- **Lifetimes and scope** — why the mediator is scoped and non-configurable, what the root-scope exception means, and how to dispatch correctly from a background service.
- **Runtime-typed dispatch** for outbox and queue consumers.
- **Trim and ahead-of-time support**, and what the runtime scanning fallback costs.
- **The deliberate absence of streaming**, with the reasoning, so it is a closed question.
- **Variance.** State plainly that the response type parameter can never be variant: it sits inside a task on the handler interfaces, and in *both* an argument position and the return position on the behavior interface. Polymorphism is request-side only, permanently, in every interface in the library. This is the single most likely thing for a future contributor to attempt and fail at.

Two diagrams via the diagram-generation skill, where they add something prose cannot: the behavior and stage composition showing short-circuit and reverse unwind, and generator module aggregation across assemblies. No diagram for anything a sentence already covers.

## Acceptance criteria

- [ ] Every section above is present and written from the developer's perspective
- [ ] The recommended chain appears with its rationale, clearly marked as opt-in rather than default
- [ ] The variance explanation states that response-side variance is impossible and why
- [ ] Code examples compile against the shipped API
- [ ] Two diagrams render correctly in both light and dark themes
- [ ] A migration note covers the removed registration API and the response-type constraint
- [ ] No documentation outside the web package references an HTTP status code
- [ ] Documentation matches the shipped behavior — no aspirational statements

## Blocked by

17 — Trim and AOT hardening
18 — Sample application
