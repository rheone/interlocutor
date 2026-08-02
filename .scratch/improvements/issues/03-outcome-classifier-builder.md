## Labels

ready-for-agent

## What to build

The default outcome describer classifies failures against a hardcoded list of five non-generic types. It already misses the generic and aggregate case types, and it structurally cannot honour the documented promise that consumers may use their own outcome shapes without adopting the kernel's types.

Replace the hardcoded classification with a builder that produces an immutable classifier. The builder should support registering a type as success or failure, and offer a predicate escape hatch for shapes that cannot be expressed by type alone. Seed it with every kernel case type using open-generic matching, so new arities cannot silently regress.

This slice covers the classifier and the describer only — wiring it into the behaviors is a separate slice.

Open decision to settle here: what an unregistered type classifies as. The recommendation is fail-closed, meaning unknown counts as failure, so a consumer who forgets to register a custom outcome sees immediate loud rollbacks rather than silently committed writes.

## Acceptance criteria

- [ ] A builder produces an immutable classifier; registration covers success types, failure types, and predicates
- [ ] Every kernel case type is recognised, including generic and aggregate forms, via open-generic matching
- [ ] Compiler-generated union values are unwrapped before classification
- [ ] The unregistered-type policy is implemented and documented
- [ ] Tests are written at the describer's public seam, red before green

## Blocked by

- 02-conflicts-and-plural-accessors
