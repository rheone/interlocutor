## Labels

ready-for-agent

## What to build

Bring the recorded decisions in line with what this effort actually decided, so every later ticket is written against accurate vocabulary and accurate architecture history.

Three existing ADRs contradict the plan or the shipped code:

- **ADR-0002 (behavior onion model)** - amend, do not supersede. Its rejection of pre/post processors *as a replacement for* behaviors still stands. What it did not anticipate is the pipeline stage abstraction that ships today: an ergonomic layer that composes *into* the behavior chain through a single host behavior, never forming a second pipeline. Record that distinction and the short-circuit contract.
- **ADR-0003 (mediator as service locator)** - supersede. Its own listed alternative, source-generated registration, is now being taken - for build-time diagnostics and AOT compatibility, not for the startup-cost reason it anticipated. The superseding ADR must distinguish "the container is the handler registry" (still true) from "handlers may locate services themselves" (never true, and not what the old title meant).
- **ADR-0007 (publish strategies)** - amend. It claims handlers execute in registration order; ordering is now explicitly unspecified, with relative run-after/run-before attributes as the escape hatch.

ADR-0002 and ADR-0005 are also written against result type names that no longer exist; correct the terminology while amending.

Then write the new ADRs this effort produces: compile-time discovery with per-assembly modules and AOT as a goal; no default pipeline with a flat ordered filtered behavior list; dispatch scope covering notification fan-out, open generics, and ambiguity as a build error; mediator scoped and non-configurable with a root-scope guard; HTTP-agnostic core with pluggable mapping; responses constrained to the outcome interface with streaming excluded.

Finally, correct the glossary. It currently documents types that do not exist and ties result types to a transport the core should know nothing about:

- Remove every HTTP status reference.
- Remove the rule-violation row - it was only ever an example.
- Mark the paged result and paged query rows as illustrations of union payloads a consumer defines themselves, or drop them.
- Correct the failure type row: it is a simple record struct carrying a reason and an error code, not a closed union of five cases. The individual failure cases are independent types composed into an outcome union.
- Rename "void command" to "unit command" - those handlers always return an outcome.

## Acceptance criteria

- [ ] ADR-0002 amended with the stage-versus-behavior distinction and the short-circuit contract; its original rejection reasoning preserved
- [ ] ADR-0003 marked superseded, with a new ADR recording the real reason for compile-time discovery
- [ ] ADR-0007 amended: ordering unspecified, relative ordering attributes described
- [ ] Stale result type names corrected in ADR-0002 and ADR-0005
- [ ] Six new ADRs written and numbered
- [ ] Glossary carries no HTTP status references
- [ ] Glossary rows for types that do not exist are removed or explicitly marked as examples
- [ ] Failure type row corrected; "void command" renamed to "unit command" throughout
- [ ] All existing tests still pass

## Blocked by

None - can start immediately
