Status: needs-triage
Severity: LOW
Category: documentation
Source: documentation.md lane, confirmed by antagonist lane

# `maintainer_docs/README.md` links to a `PRD.md` section heading that no longer exists

## Summary

The "Historical: bootstrapreference/" note links to `PRD.md#15-comparison-with-bootstrap-prd`, but `PRD.md`'s section 15 is now titled "Architecture decisions" with no "Comparison with Bootstrap PRD" heading anywhere in the file — the anchor 404s.

## Evidence

`maintainer_docs/README.md:63`: "...has been reconciled into [PRD.md §15](PRD.md#15-comparison-with-bootstrap-prd), which is the current source of truth." `maintainer_docs/PRD.md` line 180: `## 15. Architecture decisions`. `rg -in "bootstrap" maintainer_docs/PRD.md` returns only that one line, not under any "Comparison" heading.

## Suggested action

Point the link at the real heading (`PRD.md#15-architecture-decisions`), or update the surrounding sentence if section 15's content has moved/been retitled since this line was written.
