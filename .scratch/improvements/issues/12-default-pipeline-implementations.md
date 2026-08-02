## Labels

ready-for-agent

## What to build

Three abstractions injected by the pipeline behaviors have no implementation anywhere in the source tree: the audit sink, the policy evaluator, and the request context accessor. The auditing and authorization behaviors therefore cannot be resolved by any container unless the consumer hand-writes an implementation first.

Ship a usable default for each, so the documented pipeline works out of the box.

The request context accessor's web-framework-backed implementation is constrained by the enforced package layering rule, which forbids the web package from depending on the abstractions package. Either relax that rule deliberately — updating the architecture test and the layering documentation together — or introduce a package that can legitimately depend on both. Do not silently weaken the architecture test.

## Acceptance criteria

- [ ] A default audit sink records entries through the logging abstraction, and a no-op sink is available
- [ ] A default policy evaluator suitable for development and testing exists, alongside a delegate-based variant
- [ ] A framework-agnostic ambient request context accessor exists, scoped per operation and null-safe when unpopulated
- [ ] A web-framework-backed accessor populates the context from the authenticated caller
- [ ] The layering decision is made explicitly, with the architecture test and layering documentation updated together
- [ ] Tests are red before green

## Blocked by

- 04-classifier-drives-transactions
