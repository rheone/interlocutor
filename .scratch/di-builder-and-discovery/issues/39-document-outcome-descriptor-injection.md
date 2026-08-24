> **Status: done** - merged in `2183de3` (document IResponseOutcomeDescriber and OutcomeClassifierBuilder).

## Labels

ready-for-agent

## What to build

`documentation/pipeline-and-behaviors.md` currently has no coverage of `IResponseOutcomeDescriber` or `OutcomeClassifierBuilder`, even though both already ship and are already injectable/overridable - this was an open `docs/TODO.md` item ("injectable builder pattern for handling OperationOutcome options?") that turned out to already be implemented, just undocumented.

Cover, from the developer's perspective:

- **`IResponseOutcomeDescriber`**: what it does (describes and classifies a response as success/failure for logging, auditing, and transaction commit/rollback decisions), and that `LoggingBehavior`, `AuditingBehavior`, and both `TransactionBehavior` implementations already accept it as an optional constructor parameter.
- **Overriding the default**: `InterlocutorBuilder.OverrideOutcomeDescriber<TDescriber>()` - when and why a consumer would replace the default (e.g., a custom union type the default classifier doesn't recognize).
- **`OutcomeClassifierBuilder`**: the fluent builder for registering how custom success/failure types are classified - `RegisterSuccessType<T>(label)`, `RegisterFailureType<T>(label)` / `RegisterFailureType(Type, label)`, `RegisterPredicate(...)`, `.Build()`. Include a worked example building a classifier for a custom union type (not `OperationOutcome`) and wiring it in via `OverrideOutcomeDescriber`.

Once this lands, remove the corresponding line from `docs/TODO.md` (already done as part of this ticket batch - see below).

## Acceptance criteria

- [x] `IResponseOutcomeDescriber`'s role and its consumers (Logging/Auditing/TransactionBehavior) are documented
- [x] `InterlocutorBuilder.OverrideOutcomeDescriber<TDescriber>()` is documented with an example of when to reach for it
- [x] `OutcomeClassifierBuilder`'s full fluent surface is documented with a worked example for a custom union type
- [x] Every code example compiles against the shipped API
- [x] Follows house conventions from ticket 21 (alerts, TBD markers, no-counts rule, no internal design-record citations)

## Blocked by

None - can start immediately
