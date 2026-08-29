> **Status: done**

## Labels

ready-for-agent

## What to build

Link-fix + light accuracy pass over `maintainer_docs/agents/domain.md`, `maintainer_docs/agents/issue-tracker.md`, `maintainer_docs/agents/triage-labels.md`, `maintainer_docs/research/native-aot-and-trimming-options.md`, and everything under `maintainer_docs/bootstrapreference/`. Preserve `research/native-aot-and-trimming-options.md`'s forward-looking framing (it documents what a future Native AOT effort would need, per ADR-0016) - improve clarity, don't strip the future-facing content. `bootstrapreference/` is explicitly marked superseded/historical in `maintainer_docs/README.md` - fix only broken links from the rename, leave the historical content otherwise untouched.

## Acceptance criteria

- [x] All links in these files resolve post-rename (verified programmatically; `bootstrapreference/Union Types.md`'s links to `../proposals/unions` etc. are intentionally external-relative - it's a verbatim copy of a Microsoft docs page, never meant to resolve inside this repo)
- [x] `research/native-aot-and-trimming-options.md`'s forward-looking content is preserved, not removed - no changes made
- [x] `bootstrapreference/` content is unchanged beyond link fixes - no changes needed this unit (the one real link fix, ADR-0016's citation, already happened in unit 01)

## Notes

Spot-checked `native-aot-and-trimming-options.md` and the three `agents/*.md` files against every bug pattern found elsewhere this sweep (wrong attribute/type names, `.IsSuccess`/`.Failure` on the bare marker, retired package names) - none present. No edits made; the files were already accurate.
