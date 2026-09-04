# 02 - Document the `ResultTypes` naming convention

**What to build:** `Interlocutor.CommonKernel.ResultTypes` already follows a consistent, deliberate naming pattern - singular default case types (`Conflict`, `NotFound`, `Failure`), plural aggregate variants for collecting several of the same failure (`Conflicts`, `Failures`, `ValidationErrors`), and `I<Concept>FailureCase` marker interfaces (`IForbiddenFailureCase`, `IRateLimitedFailureCase`, `ITimeoutFailureCase`, `IValidationFailureCase`, `IFieldValidationFailureCase`) that let a consumer substitute their own type for that case, mirroring the `IOutcome` extension-point shape one level down. Nothing about this pattern is unclear once stated, but it isn't written down anywhere - a reader has to infer it by reading several files side by side.

Add a short, explicit callout to `documentation/messages-and-result-types.md`'s `## Result Types` section (or an immediately-adjacent subsection) naming the convention: what the singular/plural split means, when to reach for a plural aggregate instead of collecting a `List<T>` of the singular yourself, and what implementing an `I<Concept>FailureCase` marker buys a consumer (substitutability with the pipeline behaviors that already depend on it). No type is renamed by this ticket.

**Blocked by:** Ticket 01 (touches the same file - landing the rename first avoids rework/merge noise).

**Status:** done (already implemented)

- [x] `documentation/messages-and-result-types.md` states the singular-default / plural-aggregate / `I<Concept>FailureCase`-marker naming convention explicitly, in prose, near the existing `## Result Types` walkthrough of `Success`, `NotFound`, `Forbidden`, `ValidationError`, `Conflict`, `Failure`, `Cancelled`, `Timeout`, and `RateLimited`.
- [x] The callout cross-references the existing `I<Concept>FailureCase` marker interfaces and the pipeline behaviors that already depend on them, rather than re-explaining substitutability from scratch.
- [x] `documentation/glossary.md` gains (or updates) an entry pointing a reader to this convention if one doesn't already cover it.
- [x] No `ResultTypes` type is renamed, added, or removed by this ticket - documentation only.
- [x] `dotnet build` and `dotnet test` pass for the whole solution (no source changes expected, but confirms nothing was accidentally touched).

## Comments

**2026-09-04** - Re-triaged while working the frontier: already implemented. `documentation/messages-and-result-types.md`'s `## Result Types` section carries a `[!NOTE]` callout naming the singular-default/plural-aggregate/`I<Concept>FailureCase`-marker convention, cross-referencing `OutcomeMapperBase`/`DefaultHttpOutcomeStatusMapper` rather than re-explaining substitutability. `documentation/glossary.md` has a "Singular/plural/marker naming convention" entry pointing back to it. No source changes were needed - Status flipped without a code commit.

