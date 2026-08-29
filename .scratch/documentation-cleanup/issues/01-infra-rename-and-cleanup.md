> **Status: done**

## Labels

ready-for-agent

## What to build

Rename `docs/` to `maintainer_docs/` (git-mv, preserving history) and fix every repo-wide reference to the old path - `CLAUDE.md`, `README.md`, `documentation/index.md`, `documentation/messages-and-result-types.md`, `scripts/smoke-test-trimmed-sample.sh`, every `<see href="...docs/adr/...">` XML-doc link and `.csproj` comment under `src/` and `samples/`. Delete the two stale leftover directories `src/Interlocutor.EntityFrameworkCore` and `src/Interlocutor.NHibernate` (empty `bin`/`obj`, no `.csproj`, artifacts of the rename to `Interlocutor.Persistence.*`). Add a new "Documentation maintenance" entry to CLAUDE.md's Agent skills section pointing at a new `maintainer_docs/agents/documentation-maintenance.md`, and a new glossary-linking bullet to CLAUDE.md's House Style Content Rules. Write this backlog (`.scratch/documentation-cleanup/PRD.md` and this ticket set).

## Acceptance criteria

- [x] `rg 'docs/' -g '!maintainer_docs/**' -g '!.git/**' -g '!skills-lock.json'` returns nothing left unrenamed
- [x] `dotnet build` succeeds with no new errors
- [x] `src/Interlocutor.EntityFrameworkCore` and `src/Interlocutor.NHibernate` no longer exist
- [x] CLAUDE.md has a "Documentation maintenance" Agent-skills entry and a glossary-linking House Style bullet
- [x] `maintainer_docs/agents/documentation-maintenance.md` exists and is linked from `maintainer_docs/README.md`
- [x] `.scratch/documentation-cleanup/PRD.md` and `issues/01`-`12` exist

## Notes

`maintainer_docs/agents/domain.md`'s ADR-location bullet was updated to say `maintainer_docs/adr/` explicitly, since generic installed skills (`setup-matt-pocock-skills`, `improve-codebase-architecture`) still default to `docs/adr/` - see the PRD's "Known follow-up" section.
