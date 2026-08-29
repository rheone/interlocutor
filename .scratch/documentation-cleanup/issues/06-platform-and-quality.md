> **Status: open**

## Labels

ready-for-agent

## What to build

Cleanup pass over `documentation/trim-and-aot-support.md` and `documentation/testing.md` following the standing decisions in `../PRD.md`. `trim-and-aot-support.md` should state current trim-compatible-not-AOT-compatible status as fact without citing `maintainer_docs/adr/0016-...` (house style forbids internal-record citations from `documentation/`) - link sideways to `handler-selection-and-dispatch.md` or restate the reasoning directly instead. Verify testing guidance against the actual test projects' patterns (xunit.v3, NSubstitute).

## Acceptance criteria

- [ ] No stale language remains
- [ ] No citation to `maintainer_docs/` paths
- [ ] Testing guidance matches actual test project conventions
- [ ] Glossary-first-mention links added where appropriate
