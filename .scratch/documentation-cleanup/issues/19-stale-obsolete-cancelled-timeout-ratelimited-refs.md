Status: done - fixed in ce515d2

## Labels

ready-for-agent

## What to build

Three docs still describe `Cancelled`/`Timeout`/`RateLimited` as carrying `[Obsolete(...)]` in source - but that attribute was deliberately removed from `src/Interlocutor.CommonKernel/ResultTypes/` by `.scratch/build-warnings-cleanup/issues/14-remove-stale-obsolete-ratelimited-timeout.md` (merged, commit `56141a9`). The docs never caught up. These three types are current, un-obsoleted case types today; remove every claim that says otherwise.

- `documentation/glossary.md:32-34` (table rows for `Cancelled`, `Timeout`, `RateLimited`) - drop "**Obsolete** - see the warning below" from each row.
- `documentation/glossary.md:36-37` - remove/rewrite the `[!WARNING]` block claiming these three are `[Obsolete(...)]`-marked and may be removed before first stable release.
- `documentation/messages-and-result-types.md:258-259` - remove/rewrite the matching `[!WARNING]` block.
- `maintainer_docs/PRD.md:60` - drop "**all three `[Obsolete]`**, may be removed before first stable release" from the table row.
- `maintainer_docs/PRD.md:95-97` - drop the "(obsolete)" annotation on `Cancelled`/`Timeout`/`RateLimited` in the pipeline-behaviors "Produces" column.

Before editing, run `rg -n "Obsolete" documentation/ maintainer_docs/PRD.md` to confirm no other stale reference was missed by the original fact-finding sweep.

## Acceptance criteria

- [ ] All five locations above updated; no remaining doc claims these three types are `[Obsolete]` or may be removed pre-1.0
- [ ] `rg -n "Obsolete" documentation/glossary.md documentation/messages-and-result-types.md maintainer_docs/PRD.md` returns no hits referencing `Cancelled`/`Timeout`/`RateLimited`
- [ ] No change to `src/` (the attribute is already gone there - this is a docs-only fix)
- [ ] `dotnet build` unaffected (confirms no accidental code touch)

## Blocked by

None - can start immediately.

## Comments
