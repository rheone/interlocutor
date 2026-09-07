# ADR-0022: Outcome Case Classification Retired - No Universal Success/Failure Judgment

**Status:** Accepted

> [!NOTE]
> **Numbering note.** This ADR claims 0022 as the next number available in `maintainer_docs/adr/` at the time it was written (highest existing file was 0021). `.scratch/pipeline-case-fidelity/issues/02-write-adr-case-construction-fail-fast.md` also names itself "Write ADR-0022" for an unrelated decision (pipeline behavior case-construction fail-fast) and was still `ready-for-agent` - not started - at that same time. Whichever of the two ADRs lands second must renumber itself to the next free number instead of overwriting this file.

## Context

Before this decision, `IOutcomeCase.IsSuccess` and `IOutcomeClassifier.IsFailure` let framework code ask a case type or a resolved case value one question - "is this success or failure?" - and get a single, universal answer back. Two consumers depended on that answer:

- `Interlocutor.Persistence.Abstractions.CommitGateDriver` used `IsFailure` to decide whether to commit or roll back a pipeline's pending storage work at dispatch completion.
- `Interlocutor.AspNetCore`'s `ResultExtensions.ExtractCase` used the same classification, alongside `IOutcomeCaseRegistry`, to decide how to map a response's active case to an HTTP result.

The premise those two consumers shared - that every case type carries one context-independent success/failure meaning - does not hold. A `ValidationErrors` case is a failure for an HTTP status mapper (422) and, arguably, a "don't commit" signal for a naive transaction gate - but a handler can just as legitimately treat a partial-validation result as something to persist and report, not roll back. `Conflict` might mean "someone else's write already landed, nothing more to do here" - not obviously a rollback candidate either. No case type - not `Success`, not `Conflict`, not `ValidationErrors`, not any other - can be classified in the abstract; only the specific consumer asking the question, at the specific call site, knows what "success" means for its own purpose. A single `IsSuccess`/`IsFailure` bit forced every consumer to agree on one answer for a question that only makes sense scoped to the asker.

This ADR records the decision already carried out across three shipped changes:

- `.scratch/outcome-classifier-retirement/issues/01-drop-issuccess-isfailure.md` - removed `IOutcomeCase.IsSuccess` and `IOutcomeClassifier.IsFailure` (commit `d065429`).
- `.scratch/outcome-classifier-retirement/issues/02-commit-gate-explicit-disposition.md` - gave `CommitGateDriver` a commit-by-default rule with an explicit no-commit override (commit `8581b00`).
- `.scratch/outcome-classifier-retirement/issues/03-aspnetcore-throw-on-unmappable.md` - made `Interlocutor.AspNetCore`'s case-to-HTTP-result mapping throw on an unmappable case instead of guessing (commit `311ce89`).

## Decision

### No case type carries a universal success/failure meaning

`IOutcomeCase.IsSuccess` and `IOutcomeClassifier.IsFailure` are **removed, not deprecated**. This project is pre-1.0 (see [ADR-0009](0009-no-default-pipeline.md)'s consequences, and [ADR-0018](0018-unit-of-work-as-transaction-owner.md)'s own breaking removal of `TransactionBehavior`) - a straight removal with no `Obsolete` shim is consistent with the project's existing stance of accepting a breaking change over carrying a compatibility layer nobody asked for pre-release. `OutcomeMemberEmitter` no longer emits an `IsSuccess` member onto generator-attributed `Outcome<...>` unions either, so the judgment is gone from both the hand-written and generated surfaces, not just one.

`IOutcomeClassifier`/`OutcomeClassifierBuilder` are not removed - they narrow to a pure label registry. `RegisterSuccessType`/`RegisterFailureType` collapse into a single `RegisterType` (generic and by-`Type` overloads), and `RegisterPredicate` drops its `isFailure` parameter. What remains is an identity/label surface (`ActiveCaseType`, `GetLabel`) - useful for logging and diagnostics, where naming which case is active is exactly what's wanted, and categorically different from asserting that name implies success or failure. `DefaultResponseOutcomeDescriber` (the log-line renderer both `CommitGateDriver` and the store-specific `UnitOfWorkBehavior`s call) is retrofitted onto this same narrowed surface - it names the active case in a debug log line, it does not classify it.

### Case construction is a separate axis, unaffected by this decision

`IOutcomeCaseRegistry` and `ConstructOrThrow` (`OutcomeCaseRegistryExtensions.cs`) answer a different question from the one this ADR retires: not "is this case success or failure?" but "can a `TResponse` union actually hold this case, and if so, construct it." That question has nothing to do with classification and nothing here changes it - `ExtractCase` (below) still calls into the registry to resolve a response's active case; it simply no longer asks that resolved case whether it's a failure. A pipeline behavior's own case-construction-must-fail-fast question is tracked separately (see the numbering note above).

### Transaction commit/rollback: an explicit signal, never response-type inspection

`CommitGateDriver` no longer takes a classifier at all. Its rule is **commit by default**: once a dispatch has an active transaction with pending work at completion, it commits unconditionally, unless overridden by one of exactly two explicit signals:

- **A call-site override** - `Interlocutor.Abstractions.Pipeline.NoCommit.With`/`NoCommit.AndEject`, an `AsyncLocal<StrongBox<bool>?>`-backed signal a handler pairs with the specific response value it is already returning or ejecting, completely independent of what case that value carries. `CommitGateDriver` learns whether either was called via the flag `NoCommit.BeginDispatch()` hands back before running the next pipeline delegate, and turns a set flag into a `RollbackAsync` call once that delegate returns.
- **`IUnitOfWork.CancelAsync`** - an ambient call that rolls back and clears the session immediately, before `CommitGateDriver`'s own pending-work check ever runs, so a cancelled dispatch takes the "nothing to finalize" path, not the rollback path.

An unhandled exception is a third, implicit form of override: it propagates past `CommitGateDriver` untouched, leaving whatever transaction was opened uncommitted for the underlying store session to roll back on disposal. No response case type is ever inspected to make this decision - not `Success`, not `Conflict`, not `ValidationErrors`, none of them. This is a direct continuation of [ADR-0018](0018-unit-of-work-as-transaction-owner.md), not a reversal of it: ADR-0018 already established `IUnitOfWork` as transaction owner, begin-on-first-write, and `Cancel` (now `CancelAsync`) as a rollback escape hatch, describing automatic completion as "unchanged in kind from today's classifier-driven rule: commit if the classifier reports success, roll back on a classified failure." This ADR is what replaces that classifier-driven rule now that the classifier no longer answers a success/failure question - the begin-on-first-write session model, `ITransactionalRequest`'s narrowed eager-begin meaning, `IExternalStoreRequest`'s bypass, and whole-session protection are all unchanged.

### AspNetCore case-to-HTTP mapping: throw, don't guess

`Interlocutor.AspNetCore`'s `ResultExtensions.ExtractCase` no longer calls a classifier either. It now treats only the bare `Success` case, or a resolved active case with a legitimately null value, as non-failure; every other response - including one `IOutcomeCaseRegistry` cannot resolve to a known case at all - is fail-closed. When a response's active case genuinely cannot be mapped, `ExtractCase` throws a new `OutcomeCaseNotMappableException` (carrying the response type and the unmappable case's runtime type) instead of guessing at a default HTTP outcome. This mirrors the commit-gate decision above in spirit: an ambiguous case is a bug to surface loudly, not a shape to classify past.

## Alternatives considered

- **Fold `IsSuccess` into `IOutcome` as a required member**, so every case - not just ones an `IOutcomeClassifier` happens to register - carries the judgment directly. Rejected: this still asserts a universal, context-independent success/failure meaning for every case type; moving the bit onto `IOutcome` itself changes who states the judgment, not the fact that a single judgment is being stated at all. The premise in Context - that classification only makes sense scoped to a specific asker at a specific call site - applies exactly as much to a member on `IOutcome` as to a member on `IOutcomeCase`/`IOutcomeClassifier`.
- **Eliminate `ITransactionalRequest` outright**, on the theory that an explicit no-commit/`CancelAsync` signal plus begin-on-first-write makes the marker's remaining "eager begin" meaning unnecessary. **Deferred, not rejected** - left as an open research question rather than decided here. In particular, whether a validator-time database read can safely share the handler's later transaction without `ITransactionalRequest`'s eager begin is not resolved by this ADR; see `.scratch/outcome-classifier-retirement/issues/07-research-lazy-session-lifetime.md`'s own open research question for where that thread is tracked. No detail beyond flagging this as open is asserted here.

## Consequences

- **Positive:** `CommitGateDriver` and `ExtractCase` each answer their own question in their own vocabulary (commit-vs-rollback; HTTP-status-vs-unmappable) instead of both deferring to one shared, necessarily-lossy success/failure bit.
- **Positive:** "Commit by default, override only via an explicit signal" is simpler to reason about at a call site than "commit unless the classifier says this case counts as failure" - the override is visible exactly where a handler makes the decision, not implied by which case type it happens to return.
- **Positive:** `OutcomeCaseNotMappableException` surfaces an unmappable case immediately and loudly, instead of `ExtractCase` silently guessing a default HTTP outcome that might be wrong.
- **Negative:** Breaking change for any consumer that called `IOutcomeCase.IsSuccess`, `IOutcomeClassifier.IsFailure`, `OutcomeClassifierBuilder.RegisterSuccessType`/`RegisterFailureType`, or `RegisterPredicate`'s `isFailure` parameter. No migration shim ships, consistent with this project's pre-1.0 stance.
- **Negative:** A handler that wants a case-specific commit/rollback or HTTP-mapping rule must now say so explicitly (via `NoCommit`/`CancelAsync`, or by ensuring its case type is registered/mappable) rather than relying on a framework-wide default classification - more to state per call site, in exchange for no longer being wrong for the call sites where the shared default didn't fit.
- **Negative:** Whether `ITransactionalRequest` can be eliminated outright stays unresolved; a future ADR is still owed once the validator-time-read research question above is settled.
