# 01 - Opt-in success/failure classification surface for custom Outcome unions

**What to build:** `[GenerateOutcomeMembers]` unions should only get the outcome-classification surface (`IOutcomeCase`, `IsSuccess`, `ActiveCaseType`) when the union's own declaration includes `: IOutcome`. A union declared without `: IOutcome` should still get the generated sum-type members (`Match`, `Switch`, `TryGetValue`, equality, `ToString`) but none of the classification surface - it's a plain tagged union with no success/failure opinion. The shipped `Outcome<TSuccessResult, ...>` family and any existing custom union that already declares `: IOutcome` must produce byte-identical generated output to today - this is purely additive/permissive, not a breaking change.

This closes a real gap, not just a cosmetic one: `OutcomeClassifierBuilder`'s failure classifier falls back to positional `IsSuccess` for any union whose active case type isn't otherwise registered, so a non-success-shaped custom union used as a handler response today gets silently, meaninglessly classified by `LoggingBehavior`/`AuditingBehavior`. A union that doesn't implement `IOutcomeCase` at all never enters that fallback path.

**Blocked by:** None - can start immediately

**Status:** done

- [x] A `[GenerateOutcomeMembers] partial union` declared with `: IOutcome` generates `IOutcomeCase`, `IsSuccess`, and `ActiveCaseType` exactly as today.
- [x] A `[GenerateOutcomeMembers] partial union` declared without `: IOutcome` compiles and generates `Match`/`Switch`/`TryGetValue`/equality/`ToString`, but no `IOutcomeCase`, `IsSuccess`, or `ActiveCaseType` members.
- [x] A plain (non-`IOutcome`) union used as a handler response never enters the classifier's positional-`IsSuccess` fallback path, since it doesn't implement `IOutcomeCase`.
- [x] All existing tests for the shipped `Outcome` family (2-6 arity) and the existing custom-arity example (`PaymentOutcome`/its exercising tests) pass unchanged.
- [x] `documentation/extensibility.md`'s custom-arity section explains when to include vs. omit `: IOutcome` on a custom union, including the classifier-fallback rationale.

## Comments

Added `OutcomeUnionModel.ImplementsIOutcome` (resolved semantically in `OutcomeUnionModelBuilder` against `Interlocutor.CommonKernel.ResultTypes.IOutcome`'s metadata name, never by matching base-list source text) and gated `OutcomeMemberEmitter`'s `IOutcomeCase` base-interface, `IsSuccess`, and `ActiveCaseType` emission on it. The private `GetActiveCaseIndex()` helper stays unconditional - `TryGetValue`/`Match`/`Switch` all depend on it regardless of classification opt-in. Two new tests in `OutcomeMemberGeneratorTests.cs` cover a union declared without `: IOutcome`: one asserts the generated text has the sum-type surface but none of the classification surface, one asserts the emitted type never implements `IOutcomeCase` at runtime (via reflection over the compiled assembly). The shipped `Outcome` family (via `Outcome.cs`'s six `: IOutcome` declarations) and `PaymentOutcome` produce byte-identical output - full `Interlocutor.Generator.Tests` (98/98) and `Interlocutor.CommonKernel.Tests` (265/265) suites pass unchanged.
