> **Status: blocked**

Blocked by: explicit go-ahead from the repo owner (deferred during the 2026-08-29 grilling session pending a decision on format/scope - see `../PRD.md`).

## Labels

needs-info

## What to build

A full public API reference, broken down by package and namespace, covering every `src/*` package's public surface: `Interlocutor.CommonKernel`, `Interlocutor.Abstractions` (and its `.Commands`/`.Queries`/`.Core`/`.Markers`/`.Notifications`/`.Pipeline`/`.Pipeline.Cases`/`.Pipeline.Classifiers`/`.Diagnostics` sub-namespaces), `Interlocutor.AspNetCore`, `Interlocutor.DependencyInjection.Abstractions`/`.Microsoft`/`.Autofac`, `Interlocutor.FluentValidation`, `Interlocutor.Persistence.Abstractions`/`.EntityFrameworkCore`/`.NHibernate`. Excludes `Interlocutor.Generator` (compiler-internal, not a consumer-facing runtime API).

Open questions to resolve before starting (why this is blocked, not just deferred):

- One file per package under `documentation/api-reference/`, one long file with per-package sections, or generated tooling (no DocFX/Sandcastle exists in the repo today - would be new infrastructure, not just a docs pass)?
- How much detail per member - full XML-doc reproduction, or a higher-level signature-plus-summary table?
- How does this reference stay in sync with `src/` over time without becoming exactly the kind of stale documentation this whole cleanup exists to fix?

## Acceptance criteria

- [ ] Not started until unblocked
