> **Status: open**

## Labels

ready-for-agent

## What to build

Post-rename accuracy pass over `maintainer_docs/README.md`, `maintainer_docs/PRD.md`, `maintainer_docs/TODO.md`. `PRD.md` claims to be "the current specification" - verify its package structure, type catalog, and behavior descriptions still match `src/` exactly (a spec that's drifted from what shipped is worse than no spec). `TODO.md` is intentionally near-empty ("all promoted to tickets") - leave that framing, just confirm it's still true. This is maintainer-facing content, so forward-looking notes stay; only fix what's actually wrong or confusing.

## Acceptance criteria

- [ ] `maintainer_docs/PRD.md` matches current `src/` exactly wherever it makes a concrete claim
- [ ] `maintainer_docs/README.md`'s links and file list are accurate post-rename
- [ ] `maintainer_docs/TODO.md`'s "nothing outstanding" framing is confirmed still true or updated
