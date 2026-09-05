# 01 - Drop `IOutcomeCase.IsSuccess` and `IOutcomeClassifier.IsFailure`

**Status:** ready-for-agent

**What to build:** Remove `IsSuccess` from `IOutcomeCase` ([IOutcomeCase.cs](../../../src/Interlocutor.CommonKernel/ResultTypes/IOutcomeCase.cs)), keeping `ActiveCaseType` - it is still needed by `IOutcomeCaseRegistry`/AspNetCore's HTTP mapping to identify the active case, independent of any success/failure judgment. Remove `IsFailure` from `IOutcomeClassifier` ([IOutcomeClassifier.cs](../../../src/Interlocutor.Abstractions/Pipeline/Classifiers/IOutcomeClassifier.cs)) and `DefaultResponseOutcomeDescriber`/`IResponseOutcomeDescriber` ([DefaultResponseOutcomeDescriber.cs](../../../src/Interlocutor.Abstractions/Pipeline/DefaultResponseOutcomeDescriber.cs)). In `OutcomeClassifierBuilder` ([OutcomeClassifierBuilder.cs](../../../src/Interlocutor.Abstractions/Pipeline/Classifiers/OutcomeClassifierBuilder.cs)), drop `RegisterSuccessType`/`RegisterFailureType`/`RegisterPredicate`'s `isFailure` parameter and the `OutcomeClassification.IsFailure` field - the builder becomes a pure label registry (type → display label), nothing else. `AsOutcomeCaseIsSuccess()` in `OutcomeCaseExtensions` ([OutcomeCaseExtensions.cs](../../../src/Interlocutor.CommonKernel/ResultTypes/OutcomeCaseExtensions.cs)) is removed along with `IsSuccess`; `AsOutcomeCaseActiveType()` stays.

`GetLabel`/`Describe` keep their existing lenient fallback behavior unchanged (`type.Name`/`"None"`/`"Unknown"` for anything unregistered) - this ticket removes a judgment, not the labeling surface, and labeling never throws.

**Blocked by:** None - can start immediately

- [ ] `IOutcomeCase` exposes only `ActiveCaseType`; `IsSuccess` is gone
- [ ] `IOutcomeClassifier`/`IResponseOutcomeDescriber`/`DefaultResponseOutcomeDescriber` expose only the label surface (`GetLabel`/`Describe`); `IsFailure` is gone from all three
- [ ] `OutcomeClassifierBuilder`'s kernel-type seed registers labels only, no success/failure flag anywhere in its model
- [ ] `AsOutcomeCaseIsSuccess()` removed from `OutcomeCaseExtensions`; `AsOutcomeCaseActiveType()` unchanged
- [ ] Every existing test referencing the removed members is updated, not deleted wholesale, to cover the surviving labeling behavior
- [ ] `PublicAPI.Unshipped.txt` updated in `Interlocutor.CommonKernel` and `Interlocutor.Abstractions` for every removed member
- [ ] `dotnet build` and `dotnet test` pass solution-wide (this is a breaking change for the two remaining callers of `IsFailure` - `CommitGateDriver` and AspNetCore's `ResultExtensions` - see issues 02 and 03, which must land together with this one)
