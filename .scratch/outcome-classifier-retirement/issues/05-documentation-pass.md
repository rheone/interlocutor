# 05 - Documentation pass for the removed classifier surface and new commit model

**Status:** ready-for-agent

**What to build:** Update every `documentation/` page that describes the removed `IsSuccess`/`IsFailure` members or the classifier-driven commit decision:

- `documentation/messages-and-result-types.md` - any mention of `IsSuccess`/`IsFailure` as part of the generated `Outcome<...>` member set
- `documentation/pipeline-and-behaviors.md` - the `OutcomeClassifierBuilder`/`IOutcomeClassifier` walkthrough, rewritten for the label-only surface
- `documentation/transactions-and-store-integration.md` - the `UnitOfWorkBehavior` subclass example passing `IOutcomeClassifier classifier` to its base constructor (line 94), and any prose describing classification-driven commit/rollback
- `documentation/testing.md` - its own `classifier` parameter reference in a worked example
- `documentation/extensibility.md` - the `: IOutcome`/`IOutcomeCase`/`IsSuccess` explainer (line 177) describing why a union opts into classification, and the `IOutcomeClassifier`/`IResponseOutcomeDescriber` extensibility-points table (line 309) - both describe the removed `IsSuccess`-based positional fallback directly and need substantive rewriting, not a light edit
- `documentation/api-reference/interlocutor-abstractions.md` and any other generated/maintained API reference page listing the removed members

Every code example must still compile against the shipped API, per house style truthfulness rule - do not hand-wave a "conceptually similar" example past the removed members.

**Blocked by:** 01, 02, 03, 04 (documents the ADR's shipped conclusion, not a moving target)

- [ ] Every `documentation/` reference to `IsSuccess`/`IsFailure` on `IOutcomeCase`/`IOutcomeClassifier` is either removed or rewritten to reflect the label-only surface
- [ ] `transactions-and-store-integration.md`'s worked `UnitOfWorkBehavior` subclass example compiles against the new `CommitGateDriver`/`UnitOfWorkBehavior` constructor shape
- [ ] New ADR from issue 04 is cross-referenced (state the rationale itself in `documentation/`, never cite `maintainer_docs/adr/` directly, per house style)
- [ ] Glossary linking, no-enumerated-counts, and every other `documentation/` house-style rule in `CLAUDE.md` followed
