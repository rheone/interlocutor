Status: needs-triage
Severity: LOW
Category: documentation
Source: documentation.md lane, confirmed by antagonist lane

# `glossary.md` still cites ADR-0008 directly, missed by the prior ADR-citation sweep

## Summary

The `Interlocutor.Generator` glossary entry cites "(ADR-0008)" in prose, violating CLAUDE.md's "No internal design-record citations" house-style rule.

## Evidence

`documentation/glossary.md:201`: "**Interlocutor.Generator** | The Roslyn incremental source generator behind compile-time handler discovery (ADR-0008)..." A prior sweep (`.scratch/di-builder-and-discovery/issues/36-sweep-strip-internal-citations-and-fix-diagram.md`, status done) stripped ADR citations from `documentation/*.md` including two other glossary rows but missed this one. `rg -rn "ADR-" documentation/` confirms it's the sole remaining `ADR-` reference anywhere under `documentation/`.

## Suggested action

Delete the "(ADR-0008)" parenthetical — the sentence already states the rationale without needing the citation, matching how the sweep handled the other glossary rows.
