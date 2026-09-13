# 08 - Write the ADR for Eject.WithCase&lt;TCase&gt; (or record it as rejected)

**What to build:** Once issue 07's spike has a real blast-radius write-up, record the outcome as an ADR either way - accepting the design isn't the only valid result of a spike, and the decision not to proceed is exactly as worth recording as the decision to.

- If issue 07 confirms the design is worth shipping: write a new ADR (next available number after ADR-0022, i.e. `0023-eject-withcase-type-erased-variant.md`) recording the decision - the type-erased `Eject.WithCase<TCase>` shape, the wrapper-side construction move, which behaviors migrate, and the actual blast radius issue 07 measured (not an estimate). Cross-reference ADR-0022 (this stays consistent with its fail-fast principle - `Eject.WithCase` still routes through `ConstructOrThrow`, just from a different call site) and ADR-0020's already-rejected "decompose `TResponse` into a fixed failure-case generic parameter" alternative, to make clear this is a different shape (case-only, not response-type decomposition) that doesn't reopen that rejection.
- If issue 07 finds the blast radius isn't worth it: no new ADR needed - fold a short note into ADR-0022's Alternatives Considered instead, recording what was spiked and why it was dropped, so it isn't re-proposed without that context.

Nothing to decide in this ticket beyond which of the two outcomes applies - the substance comes from issue 07, mirroring `.scratch/aot-support/issues/04-write-the-adr.md`'s exact framing for the same situation (an ADR-writing ticket whose content depends entirely on a prior spike/research ticket's result).

**Blocked by:** 07

**Status:** done - implemented in `73387a6`. The commit-or-drop call had already been made directly (commit, per issue 07's own status note) outside this ticket's normal flow; this ADR records that decision and the actual partial-adoption state it shipped in. Numbered **ADR-0025**, not ADR-0022 as this ticket's text names - both ADR-0022 and ADR-0023 were already claimed by other tickets' ADRs by the time this ran.

- [x] Issue 07's write-up is read and its recommendation (commit) is followed, not re-litigated in this ticket
- [x] New ADR added under `maintainer_docs/adr/` (`0025-eject-withcase-type-erased-variant.md`), cross-referencing ADR-0023 (correct current number for the fail-fast ADR the ticket calls "ADR-0022") and ADR-0020, added to the ADR index
- [x] `.scratch/pipeline-case-fidelity/PRD.md`'s Status line is updated to reflect the outcome

## Comments
