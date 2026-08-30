> **Status: ready-for-agent** - unblocked 2026-08-30; format/scope decided by the repo owner (see Decisions below).

## Labels

ready-for-agent

## What to build

A full public API reference, broken down by package and namespace, covering every `src/*` package's public surface: `Interlocutor.CommonKernel`, `Interlocutor.Abstractions` (and its `.Commands`/`.Queries`/`.Core`/`.Markers`/`.Notifications`/`.Pipeline`/`.Pipeline.Cases`/`.Pipeline.Classifiers`/`.Diagnostics` sub-namespaces), `Interlocutor.AspNetCore`, `Interlocutor.DependencyInjection.Abstractions`/`.Microsoft`/`.Autofac`, `Interlocutor.FluentValidation`, `Interlocutor.Persistence.Abstractions`/`.EntityFrameworkCore`/`.NHibernate`. Excludes `Interlocutor.Generator` (compiler-internal, not a consumer-facing runtime API).

## Decisions (resolved 2026-08-30, repo owner deferred to recommended defaults)

- **Layout**: one file per package under `documentation/api-reference/` (e.g. `interlocutor-commonkernel.md`, `interlocutor-abstractions.md`) rather than one long combined file or generated DocFX/Sandcastle tooling - no new build infrastructure.
- **Detail level**: a signature-plus-summary table per member (member signature + its XML `<summary>`), not a full reproduction of every `<remarks>`/`<param>`/`<returns>`/`<exception>` tag - scannable, matches this repo's existing `documentation/` style.
- **Staying fresh**: hand-maintained like the rest of `documentation/`, governed by the existing `maintainer_docs/agents/documentation-maintenance.md` rule (docs updated in the same iteration as the code change that makes them stale) - no new CI check against `PublicAPI.Shipped.txt`, no "defer/snapshot" dating.

## Acceptance criteria

- [ ] One file per package under `documentation/api-reference/`, per the Decisions above, covering every `src/*` package's public surface listed above (excluding `Interlocutor.Generator`)
- [ ] Each public type/member appears as a signature-plus-summary table entry, grouped by namespace within its package's file
- [ ] Follows this repo's house style for `documentation/` (see CLAUDE.md's House Style Conventions - no enumerated counts, glossary linking on first mention, no `maintainer_docs/` citations, HTTP status codes only in `aspnetcore-integration.md`)
- [ ] Every code example/signature is verified against the shipped `src/` API, not assumed
- [ ] Linked from wherever `documentation/`'s existing index/README references the rest of the `documentation/` tree
- [ ] `dotnet build` and `dotnet test` remain green (no code changes expected, but confirms nothing was disturbed)
