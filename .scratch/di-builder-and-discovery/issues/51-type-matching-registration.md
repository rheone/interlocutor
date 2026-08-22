> **Status: done** - implemented on the `improvements` branch: `InterlocutorBuilder.AddTypeMatch(predicate, target, lifetime, except, require)`, `TypeMatchTarget` (`Handler`/`Behavior`), and `TypeMatchRule`. Matches are evaluated lazily against every candidate type found while scanning (`CandidateTypesForMatching`, the same concrete/non-abstract filter `HandlerDiscovery.Discover` uses) and exposed via the new `InterlocutorBuilder.MatchedRegistrations` property - strictly additive to `Handlers`/`Behaviors`/marker-interface-scoped behaviors, with no deduplication (two rules matching the same type as the same service at the same lifetime coexist as legal duplicates, consistent with the existing `Handler`/`Behavior` add-semantics precedent). `Require` is recorded on the rule but not enforced here - see ticket 52. Both DI packages apply `builder.MatchedRegistrations` as an additional pass alongside `Handlers`/`Behaviors`.

## Labels

ready-for-agent

## What to build

There is no first-class way to register handlers or behaviors by any criterion other than "implements a known contract interface" - a consumer wanting "every type matching some shape, register as X" has no way to express it through `InterlocutorBuilder` today, short of bypassing it and reflecting against the raw container directly.

Add a predicate-based, additive registration mechanism:

- A match rule is a predicate (`Func<Type, bool>`) tested against every candidate type found while scanning, with support for excluding specific matches from a broader rule (a carve-out - "match X except Y"). This is the reverse cardinality of the existing `OutcomeClassifierBuilder.RegisterPredicate` (which tests one runtime value against many predicates, first match wins): here, one rule is tested against many candidate types, and every type that matches gets the rule's action applied - not just the first.
- A match rule's action registers every type it matches as a handler and/or as a pipeline behavior (open or closed generic), at a lifetime the rule specifies.
- Matching zero types is legal and silent by default. A rule can opt into requiring at least one match - record this on the rule so ticket 52's `ValidateOnBuild()` can check it; this ticket does not itself throw when a required rule matches nothing.
- This is strictly additive to the existing marker-interface-based behavior scoping (`ITransactionalRequest`, `IAuditableRequest`, `IRequiresPolicy`, `IExternalStoreRequest`) - it does not replace it, and a behavior scoped by a marker interface constraint continues to work exactly as it does today whether or not any match rules are configured.
- Multiple match rules matching the same type is legal (consistent with existing duplicate-registration precedent for `Handler`/`Behavior`-kind entries) - two rules registering the same type as the same service at the same lifetime is a harmless duplicate. Two rules registering the same (service type, implementation type) pair at *different* lifetimes is not resolved here - detecting and reporting that is ticket 52's job.
- Every registration a match rule produces carries "matched by rule" provenance for ticket 50's report.

## Acceptance criteria

- [ ] A match rule registers every scanned type satisfying its predicate, minus any explicitly excluded, as a handler and/or behavior at a specified lifetime
- [ ] A match rule matching zero types does not throw or warn by default
- [ ] A match rule can be marked as requiring at least one match; this ticket records that requirement without enforcing it
- [ ] Marker-interface-based behavior scoping is unaffected - existing `AuthorizationBehavior`/`AuditingBehavior`/both `TransactionBehavior`s and their tests continue to pass unmodified in intent
- [ ] Two match rules matching the same type at the same lifetime coexist as legal duplicates
- [ ] Every registration produced by a match rule carries provenance identifying it as matched (and by which rule) in the report from ticket 50
- [ ] `dotnet build` and `dotnet test` pass for the whole solution

## Blocked by

50 - Registration report (match rules need the report model to record "matched, by which rule" provenance and to surface zero-match rules)
