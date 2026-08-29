# Documentation maintenance

How docs stay in sync with the code as the codebase evolves.

## When

Whenever a change adds, removes, or changes the behavior of anything documented - a public type or member, a pipeline behavior, a registration extension, a marker interface, a package dependency rule. Update the relevant doc file(s) in the same commit/PR as the code change. Don't defer documentation to a follow-up ticket; a follow-up ticket is where documentation debt goes to become documentation rot.

## Where

- **`documentation/`** - anything a library consumer needs: public API shape, registration, behavior composition, extensibility. Follows CLAUDE.md's House Style Content Rules (truthfulness against `src/`, no stale "old/previous" language, HTTP status codes confined to `aspnetcore-integration.md`, no citing `maintainer_docs/` paths, glossary-link on first mention).
- **`maintainer_docs/`** - anything about *why* the code is shaped the way it is, for people maintaining this repo: a new ADR for a new architectural decision, a `PRD.md` update if the shipped API surface changed, a contributor guide for a non-obvious pattern.
- A change that's purely internal (refactor with no observable behavior change) touches neither.

## How

- Use the `github-markdown` skill for the markdown itself.
- Use the `mermaid-diagram-generator` skill only where a diagram clarifies structure prose can't - not decoratively.
- Use the `csharp-docs-and-comments` skill for XML doc comments on the changed types themselves.

## Re-running a full sweep

`.scratch/documentation-cleanup/PRD.md` holds the checklist and reasoning from the last full documentation sweep across `documentation/` and `maintainer_docs/`. When drift has accumulated past what per-change updates catch - or when asked to do another full pass - re-run that same unit breakdown rather than re-deriving one from scratch.
