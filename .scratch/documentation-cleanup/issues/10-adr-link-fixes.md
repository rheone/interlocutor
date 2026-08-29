> **Status: done**

## Labels

ready-for-agent

## What to build

Mechanical pass over all 19 files in `maintainer_docs/adr/`: fix any remaining broken links (post-`docs/`→`maintainer_docs/` rename) and obvious typos only. **Do not** rewrite decision content, change an ADR's "accepted"/"superseded" status, or merge/split files - ADRs are immutable point-in-time records per `maintainer_docs/README.md`'s own stated rule.

## Acceptance criteria

- [x] Every link in every ADR resolves to a real file/heading - verified programmatically across all 19 files; none were broken (unit 01's rename pass already caught the one real case, ADR-0016)
- [x] No ADR's decision content, status, or file count changed - no edits made this unit
