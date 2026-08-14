# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

Interlocutor is a mediator/CQRS pipeline library for .NET, built on C# 15 discriminated union types (`.NET 11` preview, `EnablePreviewFeatures`). Handlers return outcomes (`Success`, `NotFound`, `Forbidden`, `Conflict`, `ValidationFailed`, ...) as compiler-checked unions rather than throwing for expected failures — exceptions are reserved for bugs/infra faults. See [README.md](README.md) for the full API walkthrough (result-type composition, pipeline behavior ordering rationale, interface segregation) and [CONTEXT.md](CONTEXT.md) for the domain glossary — read both before making result-type or pipeline-behavior changes, since the terminology and ordering rules there are load-bearing design decisions, not incidental.

## Commands

```bash
dotnet build                          # build the whole solution (Interlocutor.slnx)
dotnet test                           # run all tests (xunit.v3 + Microsoft.Testing.Platform)
dotnet test tests/Interlocutor.CommonKernel.Tests/Interlocutor.CommonKernel.Tests.csproj   # single test project
dotnet test --filter "FullyQualifiedName~DependencyTests"                          # single test/class
dotnet csharpier .                    # format (csharpier is the pinned local tool, see dotnet-tools.json)
dotnet tool restore                   # restore pinned local tools (csharpier, dotnet-outdated)
```

No solution-level build/test scripts exist beyond the `dotnet` CLI — there's no Makefile or ps1/sh wrapper.

## Architecture

**Package layering** (`src/`), enforced by `tests/Interlocutor.Architecture.Tests/DependencyTests.cs` via NetArchTest — changes that violate these boundaries fail CI:

- `Interlocutor.CommonKernel` — zero-dependency result types (`OperationOutcome<...>` unions, `Failure`/`NotFound`/`Forbidden`/`Conflict`/`ValidationFailed`/`Success`, `PagedResult<T>`). Must never depend on any other Interlocutor package.
- `Interlocutor.Abstractions` — mediator core (`IMediator`/`ISender`/`IPublisher`), handler contracts (`IQueryHandler`, `ICommandHandler`, `ResourceCommandHandler`), pipeline behaviors (`LoggingBehavior`, `AuthorizationBehavior`, `AuditingBehavior`, `PipelineStageBehavior`), marker interfaces (`ITransactionalRequest`, `IAuditableRequest`, `IRequiresPolicy`, `IExternalStoreRequest`). Depends only on CommonKernel + framework DI/Logging abstractions — must never depend on AspNetCore or the store-specific packages.
- `Interlocutor.AspNetCore` — HTTP mapping (`ToActionResult()`), web-framework integrations for Abstractions interfaces (e.g., `AspNetCoreRequestContextAccessor`). Depends on CommonKernel and Abstractions, but never on store-specific or validation packages.
- `Interlocutor.DependencyInjection.Microsoft` / `.Autofac` — assembly-scanning `AddInterlocutor()` registration per container.
- `Interlocutor.EntityFrameworkCore` / `Interlocutor.NHibernate` — store-specific `TransactionBehavior`; each must depend on Abstractions.
- `Interlocutor.FluentValidation` — `ValidationBehavior` / `BulkItemValidationBehavior` (currently mid-migration — see commented-out assertion in `DependencyTests.cs`).

Each `src/*` project has a matching `tests/*.Tests` project (xunit.v3, NSubstitute for mocking). `tests/Interlocutor.Architecture.Tests` is not per-package — it asserts the cross-package dependency rules above; add to it, not a per-package test project, when introducing a new package or changing what depends on what.

**Pipeline behaviors execute onion-style**: first `services.AddScoped(typeof(IPipelineBehavior<,>), ...)` registered runs first inbound / last outbound. The canonical order (Logging → Auditing → Authorization → Validation → Transaction) and the reasoning behind it is documented in README.md — preserve that ordering rationale when adding a new behavior rather than re-deriving it.

**Result-type convention**: don't route every handler failure through a single generic `Failure`. Pick the specific `OperationOutcome<TSuccess, ...>` arity/case types that operation can actually produce, so the compiler forces callers to handle exactly those cases (see README.md § "Result Types Are Building Blocks").

## Conventions (from AGENTS.md)

- One top-level public type per file, filename matches type name — except simple record/struct types, which may keep custom equality/formatting/operator members in the same file.
- Rich XML doc comments on all public members (doc-warning suppressions in `Directory.Build.props` are there so undocumented code doesn't break the build, not license to skip docs on new code).
- Value-added inline comments only — don't narrate obvious logic.
- Conventional commits (`feat:`, `fix:`, `chore:`...), imperative mood, subject <72 chars.

## Code style enforcement

StyleCop + Roslynator analyzers run on every build (`Directory.Build.props`); `.editorconfig` and `stylecop.json` at repo root are shared across all projects via `AdditionalFiles` linking — don't duplicate per-project style config. A specific set of StyleCop rules is suppressed solution-wide (see `NoWarn` in `Directory.Build.props`); check there before re-adding a suppression locally. `csharpier` (pinned via `dotnet-tools.json`) is the formatter — run it instead of hand-formatting.
