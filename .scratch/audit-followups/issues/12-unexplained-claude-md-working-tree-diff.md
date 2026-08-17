## Labels

needs-triage

## What to build

`CLAUDE.md` currently has an uncommitted working-tree diff that nobody in the active session(s) that produced it claims to have made deliberately - it was discovered sitting in `git status` as `M` with no corresponding conversation turn that edited it. The diff has two parts of very different character:

- An added `## Agent skills` section (Issue tracker / Triage labels / Domain docs subsections) pointing at `docs/agents/issue-tracker.md`, `docs/agents/triage-labels.md`, and `docs/agents/domain.md`. All three target files exist and the summaries are accurate against them - this part reads as a legitimate, useful addition (CLAUDE.md currently doesn't mention the `.scratch/` issue tracker or its label vocabulary anywhere, despite both being load-bearing for how work in this repo gets triaged).
- A one-line corruption: the existing `### GitHub Alerts` heading (under "House style conventions") was changed to `### GitHub MArkdown Alerts` - an inserted, mis-capitalized word that doesn't belong. This has no plausible deliberate rationale; it reads as an editing accident (bad find-replace, a stray keystroke, or a merge/rebase artifact).

This needs a maintainer's eyes because the two parts warrant different treatment and only a maintainer knows which session/process actually produced this diff:

- The typo should almost certainly just be reverted back to `### GitHub Alerts` - there's no reading of "MArkdown Alerts" that makes sense in context.
- The `## Agent skills` section's content is accurate today, but whether CLAUDE.md should permanently gain it - and whether it's complete/correctly placed relative to the rest of the file - is a content decision, not a correctness fix.

## Acceptance criteria

- [ ] The origin of the diff is understood (or explicitly given up on, if untraceable) before deciding what to do with it
- [ ] `### GitHub MArkdown Alerts` is corrected back to `### GitHub Alerts`
- [ ] A deliberate decision is made and recorded (via the commit, or a comment here) on whether the `## Agent skills` section stays, is edited, or is dropped
- [ ] `CLAUDE.md`'s working tree is clean (either committed deliberately, or the unwanted parts reverted) - it should not sit as an unexplained diff indefinitely

## Blocked by

None - can start immediately, but needs a human to actually make the content call rather than an agent guessing intent
