# 15 - Doc-citation cleanup (glossary.md + maintainer_docs/README.md)

**What to build:** Two small, unrelated but equally trivial documentation fixes, bundled into one ticket: `glossary.md`'s stray internal ADR citation is removed (per CLAUDE.md's house style), and `maintainer_docs/README.md`'s broken link to a `PRD.md` heading is repointed at the section's real, current title.

Full technical detail: [findings/14-glossary-adr-citation.md](../findings/14-glossary-adr-citation.md), [findings/15-maintainer-docs-broken-link.md](../findings/15-maintainer-docs-broken-link.md).

**Blocked by:** None - can start immediately.

**Status:** done

- [x] The `Interlocutor.Generator` glossary entry no longer cites "(ADR-0008)"
- [x] `rg -rn "ADR-" documentation/` returns no hits
- [x] `maintainer_docs/README.md`'s bootstrapreference note links to `PRD.md`'s real, current section-15 heading

## Comments

**2026-09-04** - Re-triaged while working the frontier: already implemented in commit `6a3f9ce`. glossary.md no longer cites ADR-0008 (rg -rn "ADR-" documentation/ returns no hits); maintainer_docs/README.md's bootstrapreference note now links to PRD.md's real, current §15 heading. No source changes were needed this session.
