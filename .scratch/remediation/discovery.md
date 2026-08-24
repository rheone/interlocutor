# Remediation effort

> **Status: done** - all 5 defects under "The defects that matter most" and all 6 rows of "Decisions already taken" below were independently re-verified against current `src/` (reading the actual code, not trusting this doc) and confirmed resolved/executed: the two previously-empty packages (`Interlocutor.AspNetCore`, `Interlocutor.FluentValidation`) are fully implemented with no commented-out code anywhere in `src/`; `OutcomeClassifierBuilder`/`DefaultOutcomeClassifier` drive commit/rollback in both `TransactionBehavior` implementations, fail-closed for unregistered types; the four `ImmutableArray`-backed aggregate result types (`ValidationErrors`, `ValidationErrors<TErrorCode>`, `Failures`, `Conflicts`) all guard the default-value invariant hole (`IsDefault` checks; 20/20 dedicated regression tests pass); `IAuditSink`/`IPolicyEvaluator`/`IRequestContextAccessor`/`IOutcomeClassifier`/`IResponseOutcomeDescriber` all have concrete defaults, try-added or statically-defaulted, and a real end-to-end dispatch through `UseRecommendedChain()` resolves and runs on both containers. No action needed here; historical context only. (Two unrelated, genuinely-new findings surfaced during this re-verification - a currently-failing DI integration test, and one more piece of dead code - tracked separately as `.scratch/audit-followups/issues/13-*` and an addition to `.scratch/audit-followups/issues/10-remove-dead-code.md`, not as part of this effort.)

## Why this effort exists

A three-axis review (standards / spec / correctness) of the current branch found that a nominally
docs-and-comments branch actually carries a wholesale common-kernel redesign, a core handler-interface
rename, and a new outcome-describer abstraction - alongside eight files commented out mid-migration and
never restored.

The defects that matter most:

- Two packages compile to **empty assemblies**. Their documented contracts are entirely commented out.
- Handlers returning generic or aggregate failure cases are classified as **success**, so the transaction
  behaviors **commit** writes that should have rolled back.
- The value-type conversion of the case types gave every failure type a zero value that bypasses its
  constructor, so accessing it throws.
- Three abstractions injected by the pipeline behaviors have **no implementation anywhere**, and
  registration wires up no behaviors and no describer - so the documented pipeline cannot be resolved
  out of the box.
- Requirements and glossary documents still describe the pre-migration vocabulary.

## Relationship to `.scratch/improvements/`

That effort predates the common-kernel migration and is **largely stale** - it references result and case
types that no longer exist. Superseded issues there carry a `Status: superseded` marker pointing here.
Do not pick up an issue from `improvements/` without checking that marker first.

## Sequencing

Slices are vertical: each cuts through every layer it touches and lands demoable, with its own tests
written red-first and its own documentation updates. Only the cross-cutting integration fixture and the
final requirements reconciliation are separated out, because no single implementation slice owns them.

The dependency spine is:

```
01 → 02 → 03 → 04 → {10, 11, 12} → 13 → {14, 15, 16} → 17 → 18 → 19
                 08 → 09
```

`05`, `06` and `07` are independent and can start immediately alongside `01`.

Slice `04` is the priority: it is the silent-commit defect, and it is observable end-to-end as
commit-versus-rollback.

## Decisions already taken

| Question | Decision |
| --- | --- |
| Commented-out code | Restore the HTTP mapping, validation behavior and resource command handler; delete the bulk, enumerable and paged contracts |
| Failure classification | Replace the hardcoded whitelist with a builder-configured classifier; recommendation is fail-closed for unregistered types |
| Value-type case types | Keep them as value types; guard the default-value invariant holes |
| Audit user identity | Proof-of-concept, deferred - document the gap rather than wiring it up |
| Branch history | Leave as-is; early alpha, not near release |
| Generic conflict/failure variants | Skip - both already carry an error code field |
