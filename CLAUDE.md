# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

Interlocutor is a mediator/CQRS pipeline library for .NET, built on C# discriminated union types (`.NET 11` preview, `EnablePreviewFeatures`). Handlers return outcomes (`Success`, `NotFound`, `Forbidden`, `Conflict`, `ValidationError`, ...) as compiler-checked unions rather than throwing for expected failures - exceptions are reserved for bugs/infra faults. See [README.md](README.md) for the full API walkthrough (result-type composition, pipeline behavior ordering rationale, interface segregation) and [documentation/](documentation/) for the domain glossary and reference docs - read both before making result-type or pipeline-behavior changes, since the terminology and ordering rules there are load-bearing design decisions, not incidental.

## Commands

```bash
dotnet build                          # build the whole solution (Interlocutor.slnx)
dotnet test                           # run all tests (xunit.v3 + Microsoft.Testing.Platform)
dotnet test tests/Interlocutor.CommonKernel.Tests/Interlocutor.CommonKernel.Tests.csproj   # single test project
dotnet test --filter "FullyQualifiedName~DependencyTests"                          # single test/class
dotnet csharpier .                    # format (csharpier is the pinned local tool, see dotnet-tools.json)
dotnet tool restore                   # restore pinned local tools (csharpier, dotnet-outdated)
```

No solution-level build/test scripts exist beyond the `dotnet` CLI - there's no Makefile or ps1/sh wrapper.

## Architecture

**Package layering** (`src/`), enforced by `tests/Interlocutor.Architecture.Tests/DependencyTests.cs` via NetArchTest - changes that violate these boundaries fail CI:

- `Interlocutor.CommonKernel` - zero-dependency result types (`OperationOutcome<...>` unions, `Failure`/`NotFound`/`Forbidden`/`Conflict`/`ValidationError`/`Success`). `PagedResult<T>` is not a shipped type here - it's a consumer-defined success-case type, per `documentation/glossary.md`. This remains true of `Interlocutor.CommonKernel` specifically; the optional `Interlocutor.Persistence.Abstractions` package's `Page<T>` is a different package's vocabulary for a different concern (repository return shape, not `OperationOutcome` success-case shape). Must never depend on any other Interlocutor package.
- `Interlocutor.Abstractions` - mediator core (`IMediator`/`ISender`/`IPublisher`), handler contracts (`IQueryHandler`, `ICommandHandler`, `ResourceCommandHandler`), pipeline behaviors (`LoggingBehavior`, `AuthorizationBehavior`, `AuditingBehavior`, `PipelineStageBehavior`), marker interfaces (`ITransactionalRequest`, `IAuditableRequest`, `IRequiresPolicy`, `IExternalStoreRequest`). Depends only on CommonKernel + framework DI/Logging abstractions - must never depend on AspNetCore or the store-specific packages.
- `Interlocutor.AspNetCore` - HTTP mapping (`ToActionResult()`), web-framework integrations for Abstractions interfaces (e.g., `AspNetCoreRequestContextAccessor`). Depends on CommonKernel and Abstractions, but never on store-specific or validation packages.
- `Interlocutor.DependencyInjection.Microsoft` / `.Autofac` - assembly-scanning `AddInterlocutor()` registration per container. Also carry the opt-in `AddInterlocutorUnitOfWork`/`AddInterlocutorRepository` persistence registration extensions; both depend only on `Interlocutor.Persistence.Abstractions` (zero ORM dependency), never on either `Interlocutor.Persistence.*` implementation package.
- `Interlocutor.EntityFrameworkCore` / `Interlocutor.NHibernate` - store-specific `TransactionBehavior`; each must depend on Abstractions.
- `Interlocutor.FluentValidation` - `ValidationBehavior`; `DependencyTests.cs` asserts it depends on Abstractions.
- `Interlocutor.Persistence.Abstractions` - optional, opt-in persistence abstraction (`IUnitOfWork`, `IDomainRepository<TEntity, TKey>`, `Page<T>`, `SortCriterion<TEntity>`). Zero ORM dependency; depends only on Abstractions.
- `Interlocutor.Persistence.EntityFrameworkCore` / `Interlocutor.Persistence.NHibernate` - store-specific implementations of the persistence abstraction; each must depend on `Interlocutor.Persistence.Abstractions` and must not depend on each other.

Each `src/*` project has a matching `tests/*.Tests` project (xunit.v3, NSubstitute for mocking). `tests/Interlocutor.Architecture.Tests` is not per-package - it asserts the cross-package dependency rules above; add to it, not a per-package test project, when introducing a new package or changing what depends on what.

**Pipeline behaviors execute onion-style**: first `services.AddScoped(typeof(IPipelineBehavior<,>), ...)` registered runs first inbound / last outbound. The canonical order (Logging → Auditing → Authorization → Validation → Transaction) and the reasoning behind it is documented in README.md - preserve that ordering rationale when adding a new behavior rather than re-deriving it.

**Result-type convention**: don't route every handler failure through a single generic `Failure`. Pick the specific `OperationOutcome<TSuccess, ...>` arity/case types that operation can actually produce, so the compiler forces callers to handle exactly those cases (see README.md § "Result Types Are Building Blocks").

## Conventions (from AGENTS.md)

- One top-level public type per file, filename matches type name - except simple record/struct types, which may keep custom equality/formatting/operator members in the same file. For member ordering, partial-class splitting, generic-arity file naming, and region grouping, use the `csharp-code-organization` skill.
- Rich XML doc comments on all public members (doc-warning suppressions in `Directory.Build.props` are there so undocumented code doesn't break the build, not license to skip docs on new code).
- Value-added inline comments only - don't narrate obvious logic.
- Conventional commits (`feat:`, `fix:`, `chore:`...), imperative mood, subject <72 chars.

## Agent skills

### Issue tracker

Local markdown - issues live as files under `.scratch/<feature>/`. No PR triage. See `docs/agents/issue-tracker.md`.

### Triage labels

Default labels: `needs-triage`, `needs-info`, `ready-for-agent`, `ready-for-human`, `wontfix`. See `docs/agents/triage-labels.md`.

### Domain docs

Single-context: one `CONTEXT.md` and one `docs/adr/` at the repo root. See `docs/agents/domain.md`.

## Code style enforcement

StyleCop + Roslynator analyzers run on every build (`Directory.Build.props`); `.editorconfig` and `stylecop.json` at repo root are shared across all projects via `AdditionalFiles` linking - don't duplicate per-project style config. A specific set of StyleCop rules is suppressed solution-wide (see `NoWarn` in `Directory.Build.props`); check there before re-adding a suppression locally. `csharpier` (pinned via `dotnet-tools.json`) is the formatter - run it instead of hand-formatting.

## House style conventions

Every documentation file in this directory follows these conventions:

### GitHub Markdown Alerts

Use all five GitHub alert types purposefully:

- `[!NOTE]` - supplementary detail, in-the-weeds content, or TBD placeholders (always in this form: `> [!NOTE]\n> **TBD:** <what's missing>`, greppable)
- `[!TIP]` - optional but recommended practice
- `[!IMPORTANT]` - load-bearing design decision that's easy to miss
- `[!WARNING]` - common footgun or easy mistake
- `[!CAUTION]` - severe or hard-to-reverse consequence (e.g., a constraint that can never change without a breaking version bump)

### Content rules

- **No enumerated counts in prose**: say "a set of" / "several" and link to source instead. Exception: counts are fine when enumerating an explicit, closed set of type implementations (e.g., "the five `OperationOutcome` failure cases: `NotFound`, `Forbidden`, `Conflict`, `ValidationError`, `Failure`").
- **Auto-generated counts are OK**: badges, CI test counts, etc., since they can't go stale.
- **Diagrams**: inline `​```mermaid` fences only. No standalone `.mmd` files - we don't have reuse-across-docs at scale yet.
- **HTTP status codes**: only `aspnetcore-integration.md` is permitted to reference HTTP status codes. All other docs link there instead.
- **Truthfulness**: every code example compiles against the shipped API. Claims are verified against `src/` code, not carried forward from older docs if they've been superseded.
- **No internal design-record citations**: never cite or link to `docs/adr/`, `docs/PRD.md`, `docs/TODO.md`, or any other `docs/` path to justify a design choice - e.g. don't write "See ADR-0009 for the no-default-pipeline design" or "`docs/PRD.md` §9.2 has the full design." State the rationale itself, as fact, in this doc - the *why* stays, the citation to an internal decision record doesn't. `docs/` is internal maintainer documentation (see [`docs/README.md`](../docs/README.md)); a consumer reading this tree has no reason to open it. If a claim needs a pointer, link sideways to the sibling `documentation/` file that already covers it, never to `docs/`.

## TBD sections

No documentation sections are currently blocked on pending infrastructure work. TBD placeholders are written per the alert convention above whenever one is needed, so nothing is forgotten.
