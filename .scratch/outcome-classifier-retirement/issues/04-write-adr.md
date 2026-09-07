# 04 - Write the ADR recording this decision

**Status:** ready-for-agent

**What to build:** A new ADR recording: (a) no case type carries a universal success/failure meaning - `IOutcomeCase.IsSuccess`/`IOutcomeClassifier.IsFailure` are removed, not deprecated; (b) `IOutcomeCaseRegistry`/`ConstructOrThrow` and case *construction* are unaffected - this ADR is about classification, a separate axis; (c) the transaction commit/rollback decision moves entirely to an explicit signal (call-site override + the already-shipped `IUnitOfWork.Cancel()`) plus exception propagation, never response-type inspection, with "commit by default" as the resulting rule; (d) the two alternatives raised and rejected during design - folding `IsSuccess` into `IOutcome` as a required member (rejected: still asserts a universal judgment), and eliminating `ITransactionalRequest` outright (deferred, not rejected - see issue 07's open research question about validator-time DB reads sharing the handler's later transaction).

Pick the next available ADR number. Note the sequencing risk explicitly in the ADR's own front matter if `.scratch/pipeline-case-fidelity/issues/02` (ADR-0022) has not landed yet by the time this is written - do not silently claim a number that ticket also expects to claim.

**Blocked by:** 01, 02, 03 (records their shipped outcome, not a proposal ahead of it)

- [ ] New ADR file in `maintainer_docs/adr/`, numbered correctly against whatever has landed by the time this is written
- [ ] Records the decision, the two rejected/deferred alternatives, and cross-references ADR-0018 (unit-of-work as transaction owner) as the design this continues rather than reverses
- [ ] Follows the existing ADR template/style used by `maintainer_docs/adr/0018-unit-of-work-as-transaction-owner.md` and `0020-validation-failure-case-fidelity.md`
