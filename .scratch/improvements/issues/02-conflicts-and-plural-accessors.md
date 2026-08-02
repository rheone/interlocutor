## Labels

ready-for-agent

## What to build

The singular and plural case types are asymmetric. The validation error has a generic variant and a plural aggregate; the conflict case has neither; the failure case has a plural but names its collection differently.

Add a conflict aggregate mirroring the failure aggregate, and settle on one accessor naming convention across all plural types so callers do not have to remember which one exposes which property name.

Also split the case types so file organization matches the one-type-per-file convention: singular and generic-singular forms together, plural forms together, and the outcome marker interface in its own file rather than sharing with the union definitions.

Do not add generic error-code variants of the conflict or failure cases — both already carry an error code field, and the union arities let a caller express a typed case. Record that decision.

## Acceptance criteria

- [ ] A conflict aggregate exists, de-duplicates preserving first-occurrence order, and is safe at its default value from the outset
- [ ] All plural aggregates expose the same collection accessor name; type-specific projections remain as extras
- [ ] Case-type files follow the one-type-per-file convention, with the outcome marker interface extracted
- [ ] The decision to skip generic conflict and failure variants is recorded
- [ ] The domain glossary is updated with the new and renamed vocabulary

## Blocked by

- 01-case-types-safe-at-default
