> **Status: open**

## Labels

ready-for-agent

## What to build

Link-fix + light accuracy pass over `maintainer_docs/agents/domain.md`, `maintainer_docs/agents/issue-tracker.md`, `maintainer_docs/agents/triage-labels.md`, `maintainer_docs/research/native-aot-and-trimming-options.md`, and everything under `maintainer_docs/bootstrapreference/`. Preserve `research/native-aot-and-trimming-options.md`'s forward-looking framing (it documents what a future Native AOT effort would need, per ADR-0016) - improve clarity, don't strip the future-facing content. `bootstrapreference/` is explicitly marked superseded/historical in `maintainer_docs/README.md` - fix only broken links from the rename, leave the historical content otherwise untouched.

## Acceptance criteria

- [ ] All links in these files resolve post-rename
- [ ] `research/native-aot-and-trimming-options.md`'s forward-looking content is preserved, not removed
- [ ] `bootstrapreference/` content is unchanged beyond link fixes
