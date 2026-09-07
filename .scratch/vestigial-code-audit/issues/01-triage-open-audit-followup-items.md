# 01 - Triage open audit-followup items 08/12/14/18

**What to build:** Review `.scratch/codebase-audit-followups/issues/{08,12,14,18}.md` against current `HEAD`. Each was last touched during earlier branch-landing normalization; confirm whether the underlying premise still holds (some may have already been resolved incidentally by other work since), and update each one's `Status:` line and a short rationale accordingly - kept as `ready-for-agent`, marked `done` with evidence, or closed as no-longer-applicable with an explanation.

**Blocked by:** None - can start immediately

**Status:** done

- [x] All four issues reviewed against current `HEAD`, not against their last-written description
- [x] Each one's `Status:` line reflects the current state, with a one-line rationale appended under its own `## Comments`
- [x] `.scratch/codebase-audit-followups`'s own tracking (if it has a PRD-level checklist) updated to match (no PRD-level file exists for this collection - only `issues/`, so nothing further to update)

## Comments

**2026-09-04** - Already satisfied by earlier work this same session (before this ticket was picked up): 08 and 12 were implemented in commit `5507265`, 14 was implemented in the same commit, and 18 was reviewed and closed `wontfix (reviewed, no action needed)` in commit `2da952b`. All four carry their own `## Comments` rationale. No further action needed - marking done retroactively.
