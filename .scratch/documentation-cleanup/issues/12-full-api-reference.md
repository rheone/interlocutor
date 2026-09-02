> **Status: done** - triaged and picked up 2026-09-02. Generated from the compiled `src/*` assemblies plus their
> shipped XML doc comments (Roslyn metadata symbols + `GetDocumentationCommentXml`, resolving the `<inheritdoc>`
> pointer C# 14 extension-block members compile down to) rather than hand-transcribed, so every signature and
> summary is a direct reflection of the shipped API. One deviation from a strict "just what's in the XML docs"
> reproduction: a handful of source XML doc comments cite ADR numbers inline (`ADR-0007`, `ADR-0008`, `ADR-0009`,
> `ADR-0011`, `ADR-0018`) - those citations are fine in source code comments, but reproducing them verbatim into
> `documentation/*.md` would violate this file's own house-style rule (no `maintainer_docs/` citations), so they
> were stripped/rephrased in the generated output specifically, not in the source XML comments themselves.

## Labels

needs-triage

## What to build

A full public API reference, broken down by package and namespace, covering every `src/*` package's public surface: `Interlocutor.CommonKernel`, `Interlocutor.Abstractions` (and its `.Commands`/`.Queries`/`.Core`/`.Markers`/`.Notifications`/`.Pipeline`/`.Pipeline.Cases`/`.Pipeline.Classifiers`/`.Diagnostics` sub-namespaces), `Interlocutor.AspNetCore`, `Interlocutor.DependencyInjection.Abstractions`/`.Microsoft`/`.Autofac`, `Interlocutor.FluentValidation`, `Interlocutor.Persistence.Abstractions`/`.EntityFrameworkCore`/`.NHibernate`. Excludes `Interlocutor.Generator` (compiler-internal, not a consumer-facing runtime API).

## Decisions (resolved 2026-08-30, repo owner deferred to recommended defaults)

- **Layout**: one file per package under `documentation/api-reference/` (e.g. `interlocutor-commonkernel.md`, `interlocutor-abstractions.md`) rather than one long combined file or generated DocFX/Sandcastle tooling - no new build infrastructure.
- **Detail level**: a signature-plus-summary table per member (member signature + its XML `<summary>`), not a full reproduction of every `<remarks>`/`<param>`/`<returns>`/`<exception>` tag - scannable, matches this repo's existing `documentation/` style.
- **Staying fresh**: hand-maintained like the rest of `documentation/`, governed by the existing `maintainer_docs/agents/documentation-maintenance.md` rule (docs updated in the same iteration as the code change that makes them stale) - no new CI check against `PublicAPI.Shipped.txt`, no "defer/snapshot" dating.

## Acceptance criteria

- [x] One file per package under `documentation/api-reference/`, per the Decisions above, covering every `src/*` package's public surface listed above (excluding `Interlocutor.Generator`)
- [x] Each public type/member appears as a signature-plus-summary table entry, grouped by namespace within its package's file
- [x] Follows this repo's house style for `documentation/` (see CLAUDE.md's House Style Conventions - no enumerated counts, glossary linking on first mention, no `maintainer_docs/` citations, HTTP status codes only in `aspnetcore-integration.md`)
- [x] Every code example/signature is verified against the shipped `src/` API, not assumed
- [x] Linked from wherever `documentation/`'s existing index/README references the rest of the `documentation/` tree
- [x] `dotnet build` and `dotnet test` remain green (no code changes expected, but confirms nothing was disturbed)
