> **Amendment** — the house conventions established here omitted a rule against citing internal design records (ADRs, `docs/PRD.md`) in `documentation/` prose, which is why such citations crept into every subsequent ticket's output (23–34) and into `index.md`'s own links and `glossary.md`'s two inline citations. `documentation/` files must state design rationale as fact and must never cite or link to `docs/adr/`, `docs/PRD.md`, or other `docs/` paths — that rule has been added by ticket 36, and the shipped files corrected accordingly. No action needed here; historical context only.

## Labels

ready-for-agent

## What to build

Establish a new top-level `documentation/` directory: public, developer-facing reference documentation for people *using* Interlocutor. This is deliberately distinct from the existing `docs/` directory, which stays as internal documentation about building *this repository* (PRD, TODO, ADRs, agent guidance, bootstrap reference material) — don't mix the two audiences in one tree. Every other doc ticket in this batch (22–35) writes into `documentation/` and depends on the skeleton this ticket creates.

This ticket is the prefactor for the rest of the documentation effort:

- Create `documentation/` with an index/landing file that will be linked from the README and lists every doc file this batch produces (add entries as those tickets land — this index can and should grow after this ticket merges).
- Create `documentation/glossary.md`, seeded from `CONTEXT.md`'s existing glossary content, expanded where useful, and delegating to the specific per-topic docs (tickets 23–32) rather than re-explaining a concept `CONTEXT.md` already covered in depth — a glossary entry should be a short definition plus a link, not a duplicate essay.
- Establish the house style conventions this whole documentation effort follows, written down once (in the index or a short `documentation/CONVENTIONS.md` — pick whichever reads more naturally) so every other ticket can just say "follow house conventions" instead of repeating this:
  - GitHub alert types, used purposefully: `[!NOTE]` for supplementary/in-the-weeds detail, `[!TIP]` for optional-but-recommended practice, `[!IMPORTANT]` for a load-bearing design decision that's easy to miss, `[!WARNING]` for a common footgun, `[!CAUTION]` for a severe or hard-to-reverse consequence.
  - TBD placeholders for anything not true yet, in the fixed, greppable form: `> [!NOTE]\n> **TBD:** <one-line description of what's missing>`.
  - No enumerated counts in hand-written prose (say "a set of" / "several", link to source instead) — except when enumerating an explicit, closed set of type implementations (e.g., naming the five `OperationOutcome` failure cases by name is fine).
  - Diagrams are inline `​```mermaid` fences only, no standalone `.mmd` files.
  - No documentation file outside the ASP.NET Core integration doc (ticket 31) references an HTTP status code.
- Retire `CONTEXT.md`: delete it once its content is migrated into `documentation/` (glossary plus pointers into the relevant per-topic docs as they land). No stub is left behind — this is a pre-release library with no external links to preserve.
- Update `CLAUDE.md`'s instruction to read `CONTEXT.md` before result-type/pipeline-behavior changes so it points at the new `documentation/` location instead — leaving it pointing at a deleted file would immediately break agent guidance.

While migrating content, resolve a real naming discrepancy found between the current README's package table (`ValidationFailed`/`ValidationFailure`) and `CONTEXT.md`'s package table (`ValidationError`/`ValidationErrors`) for the same case types — check `Interlocutor.CommonKernel`'s actual source to determine which name is correct today, and use that name consistently in the new glossary and everywhere downstream.

## Acceptance criteria

- [ ] `documentation/` directory exists with an index file and `glossary.md`
- [ ] House style conventions (alert types, TBD form, no-counts rule, inline-diagram-only rule, HTTP-status-code rule) are written down once and linked from every subsequent doc ticket
- [ ] `CONTEXT.md` is deleted; its content is represented in `documentation/glossary.md` and/or linked out to the relevant per-topic doc
- [ ] `CLAUDE.md`'s cross-reference to `CONTEXT.md` is updated to point at `documentation/`
- [ ] The `ValidationFailed`/`ValidationFailure` vs `ValidationError`/`ValidationErrors` naming discrepancy is resolved against actual `src/Interlocutor.CommonKernel` source, not copied from either stale doc
- [ ] `docs/` is untouched in purpose — it remains internal/repo-facing documentation

## Blocked by

None — can start immediately
