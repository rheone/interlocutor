# Full-repository review — 2026-09-02

Status: done (all 17 tickets in `issues/`) - implemented in commit `6a3f9ce` ("fix: resolve Outcome null-payload discriminator and 16 follow-on tickets"). Re-verified against current source/docs/tests on 2026-09-04 while working the frontier; all 17 ticket files updated individually.

**Start here:** [issues/](issues/) holds 17 ready-for-agent tickets, numbered in dependency order, converted from the findings below via `/to-tickets`. The frontier (no blockers) is everything except 03 and 04, which are blocked by 01. Raw review findings with full file:line evidence live in `findings/` — each ticket links back to its source finding(s) for detail.

## Methodology

Four subagents swept the entire repository in parallel, each in one lane, reading actual source rather than sampling — not a diff-based review:

- **Bugs/correctness** — `drafts/bugs.md`
- **Architecture** — `drafts/architecture.md`
- **Documentation** — `drafts/documentation.md`
- **Tests & conventions** — `drafts/tests-conventions.md`

A fifth, adversarial agent then read all four drafts, independently re-verified every finding against the actual code (including building and running throwaway tests against the real compiled library for the two CRITICAL claims, then reverting them), corrected severities where its own reading disagreed, and ran its own independent gap-hunt for cross-cutting issues no single lane could see — `drafts/antagonist.md`.

**Result: 14 of 14 original findings confirmed real (zero false positives), 2 severities adjusted, 1 root-cause correction, 4 new findings from the gap-hunt.** All 17 were then converted into tickets in `issues/` — some findings share a ticket (or a ticket covers a finding's downstream regression tests as a separate, blocked slice), so the ticket count matches the finding count without being a 1:1 mapping.

## Tickets, in dependency order

| Ticket | Blocked by | Title |
|---|---|-------|
| [01](issues/01-outcome-null-payload-discriminator.md) | None | Fix the `Outcome` null-payload/no-discriminator bug at the root — **CRITICAL** |
| [02](issues/02-fluentvalidationerrors-equality.md) | None | Fix `FluentValidationErrors` equality — **CRITICAL** |
| [03](issues/03-unitofwork-null-payload-regression-tests.md) | 01 | Regression-test transaction correctness for null-payload successes |
| [04](issues/04-aspnetcore-null-case-mapping.md) | 01 | Fix/verify AspNetCore's outcome-case mapping for null payloads |
| [05](issues/05-aggregate-type-equality-tests.md) | None | Add direct equality tests for the other 4 aggregate types |
| [06](issues/06-resourcecommandhandler-forbidden.md) | None | Give `ResourceCommandHandler` a `Forbidden` case |
| [07](issues/07-generator-wiring-docs-fix.md) | None | Reconcile `Interlocutor.Generator`'s documented wiring with reality |
| [08](issues/08-readme-quickstart-fix.md) | None | Fix README.md Quick Start example |
| [09](issues/09-dependencytests-layering-gaps.md) | None | Close `DependencyTests.cs` layering gaps |
| [10](issues/10-unitofwork-cancel-disposal.md) | None | Fix `IUnitOfWork.Cancel()` disposal asymmetry |
| [11](issues/11-pipeline-docs-fictitious-hooks.md) | None | Fix `pipeline-and-behaviors.md`'s fictitious execution-order prose |
| [12](issues/12-sample-app-walkthrough-sync.md) | None | Sync `sample-app-walkthrough.md` with TutorialProject |
| [13](issues/13-globaljson-rollforward-policy.md) | None | Harden `global.json`'s SDK roll-forward policy |
| [14](issues/14-externalstorerequest-doc-fix.md) | None | Clarify `IExternalStoreRequest`'s doc comment |
| [15](issues/15-doc-citation-cleanup.md) | None | Doc-citation cleanup (glossary.md + maintainer_docs/README.md) |
| [16](issues/16-ci-format-check.md) | None | Add a CI formatting check |
| [17](issues/17-nuget-packaging-metadata.md) | None | Fix NuGet packaging metadata |

## Headline risk

**Tickets 01, 03, and 04 are the same root cause** (no discriminator distinguishing a null case payload from an unset union) surfacing in three places: the core `Outcome` type's own members, `UnitOfWorkBehavior`'s transaction-commit decision, and `Interlocutor.AspNetCore`'s reflective HTTP mapping. Fix once at the source (ticket 01), then land the two regression-test slices. This is the single highest-priority item: it causes **silent data loss** (a successful database write rolled back) through entirely ordinary, legal C# (`Outcome<string?, NotFound>` returning `null`), with no exception visible to the caller.

**Ticket 02** is the second CRITICAL and independent of ticket 01 — a documented, tested-sounding equality guarantee (commit `d5c9a80`) that was never actually implemented for one of the six types it claims to cover.

## Raw review output

- `findings/` — the 17 synthesized, severity-corrected findings (file:line evidence, full detail) each ticket links back to.
- `drafts/` — the unsynthesized per-lane sweeps and the full adversarial critique, kept for provenance.

Work from `issues/` — that's the actionable, triage-ready output.
