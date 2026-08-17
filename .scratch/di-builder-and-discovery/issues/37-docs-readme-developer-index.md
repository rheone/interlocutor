> **Status: done** - merged in `973729c`.

## Labels

ready-for-agent

## What to build

A `docs/README.md` - the developer-documentation index, mirroring `documentation/index.md`'s role but for contributors and maintainers of this repository rather than library consumers. Today `docs/` has no index at all: a contributor lands in a folder with `PRD.md`, `TODO.md`, `adr/`, `agents/`, `bootstrapreference/`, `guides/` and no map.

Cover:

- A top-line statement that this is internal repo documentation, and that library consumers want `documentation/` instead (link to `documentation/index.md`).
- Links to `PRD.md` and `TODO.md`.
- Links to every ADR in `adr/` (13 files, 0001–0013), with their titles.
- Links to `guides/composing-operationoutcome-arities.md` and the three files in `agents/` (`domain.md`, `issue-tracker.md`, `triage-labels.md`).
- A `[!NOTE]` marking `bootstrapreference/` as historical/superseded - it predates this repo's implementation and has been reconciled into `PRD.md` §15 ("Comparison with bootstrap PRD"), which is the current source of truth. Consult it only for historical context, never as authoritative for current behavior.

## Acceptance criteria

- [ ] `docs/README.md` exists and links to `PRD.md`, `TODO.md`, every file in `adr/`, `guides/`, `agents/`
- [ ] `bootstrapreference/` is marked historical/superseded via a `[!NOTE]`, pointing at `PRD.md`'s §15 anchor
- [ ] Top line points a library consumer who lands here by mistake back to `documentation/`
- [ ] Every link resolves

## Blocked by

None - can start immediately
