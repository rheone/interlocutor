# CLAUDE.md

## Interlocutor

Interlocutor is a mediator/CQRS pipeline library for .NET, built on C# (`.NET 11` preview, `EnablePreviewFeatures`). It uses Handlers than return `IOutcome` outcomes which may be compiler-checked unions rather than throwing for expected failures - exceptions are reserved for bugs/infra faults.

See [README.md](README.md) for the full API walkthrough (result-type composition, pipeline behavior ordering rationale, interface segregation) and [documentation/](documentation/) for the domain glossary and reference docs - read both before making result-type or pipeline-behavior changes, since the terminology and ordering rules there are load-bearing design decisions, not incidental. `TutorialProject/` is a runnable worked example - a full implementation of the Beginner's Guide (vertical slices by directory, Microsoft.Extensions.DependencyInjection/EF Core/FluentValidation, a multi-repository unit-of-work example, a multi-subscriber notification) - see `documentation/sample-app-walkthrough.md` before changing it, since its shape is what that doc walks a reader through. An earlier Autofac/NHibernate version is preserved via the `tutorial-project-nhibernate-autofac` git tag rather than kept in-tree - see that doc's own note before assuming it's still there.

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

Prefer the use of ripgrep (`rg`) over grep

## Architecture

**Package layering** (`src/`), enforced by `tests/Interlocutor.Architecture.Tests/DependencyTests.cs` via NetArchTest - changes that violate these boundaries fail CI:

- `Interlocutor.CommonKernel` - zero-dependency result types (`Outcome<...>` unions, `Failure`/`NotFound`/`Forbidden`/`Conflict`/`ValidationError`/`Success`). `PagedResult<T>` is not a shipped type here - it's a consumer-defined success-case type, per `documentation/glossary.md`. This remains true of `Interlocutor.CommonKernel` specifically; the optional `Interlocutor.Persistence.Abstractions` package's `Page<T>` is a different package's vocabulary for a different concern (repository return shape, not `Outcome` success-case shape). Must never depend on any other Interlocutor package.
- `Interlocutor.Abstractions` - mediator core (`IMediator`/`ISender`/`IPublisher`), handler contracts (`IQueryHandler`, `ICommandHandler`, `ResourceCommandHandler`), pipeline behaviors (`LoggingBehavior`, `AuthorizationBehavior`, `AuditingBehavior`, `PipelineStageBehavior`), marker interfaces (`ITransactionalRequest`, `IAuditableRequest`, `IRequiresPolicyRequest`, `IExternalStoreRequest`). Depends only on CommonKernel + framework DI/Logging abstractions - must never depend on AspNetCore or the store-specific packages.
- `Interlocutor.Generator` - the Roslyn incremental source generator behind compile-time handler discovery/dispatch (see `documentation/handler-selection-and-dispatch.md`). Opt-in: a consumer who wants compile-time discovery references it directly as a `PrivateAssets="all"` analyzer package in their own project - it is not pulled in transitively by `Interlocutor.Abstractions`, which carries no reference to it. `Interlocutor.CommonKernel`'s own reference to it is unrelated (source-generating `Outcome`/`Outcome<...>`'s members for CommonKernel's own compilation, not handler discovery). A consumer who skips it uses `ScanAssembly`'s runtime-reflection discovery instead, as `TutorialProject` does - it never ships in a consuming build's output either way.
- `Interlocutor.AspNetCore` - HTTP mapping (`ToActionResult()`), web-framework integrations for Abstractions interfaces (e.g., `AspNetCoreRequestContextAccessor`). Depends on CommonKernel and Abstractions, but never on store-specific or validation packages.
- `Interlocutor.DependencyInjection.Abstractions` - container-neutral registration model (`InterlocutorBuilder`, `InterlocutorPipelineBuilder`, `RegistrationReport`) shared by the Microsoft and Autofac DI packages below. Depends only on Abstractions; must never depend on either concrete DI package or a store-specific package.
- `Interlocutor.DependencyInjection.Microsoft` / `.Autofac` - assembly-scanning `AddInterlocutor()` registration per container, built on `Interlocutor.DependencyInjection.Abstractions`. Also carry the opt-in `AddInterlocutorUnitOfWork`/`AddInterlocutorRepository` persistence registration extensions; both depend only on `Interlocutor.Persistence.Abstractions` (zero ORM dependency), never on either `Interlocutor.Persistence.*` implementation package. Neither DI package may depend on the other.
- `Interlocutor.FluentValidation` - `ValidationBehavior`; `DependencyTests.cs` asserts it depends on Abstractions.
- `Interlocutor.Persistence.Abstractions` - optional, opt-in persistence abstraction (`IUnitOfWork`, `IDomainRepository<TEntity, TKey>`, `Page<T>`, `SortCriterion<TEntity>`). Zero ORM dependency; depends only on Abstractions.
- `Interlocutor.Persistence.EntityFrameworkCore` / `Interlocutor.Persistence.NHibernate` - store-specific implementations of the persistence abstraction, and each store's `UnitOfWorkBehavior` - the unit-of-work-owned transaction commit/rollback gate that supersedes the retired `Interlocutor.EntityFrameworkCore`/`Interlocutor.NHibernate` packages' `TransactionBehavior`; each must depend on `Interlocutor.Persistence.Abstractions` and must not depend on each other.
- `Interlocutor.Testing` - `FakeSender`/`FakePublisher`, fake `ISender`/`IPublisher` implementations for a consumer's own unit tests, with no real pipeline, container, or handler registration involved. Depends only on CommonKernel + Abstractions; `DependencyTests.cs` asserts it depends on nothing else.

Each `src/*` project has a matching `tests/*.Tests` project (xunit.v3, NSubstitute for mocking). `tests/Interlocutor.Architecture.Tests` is not per-package - it asserts the cross-package dependency rules above; add to it, not a per-package test project, when introducing a new package or changing what depends on what.

**Pipeline behaviors execute onion-style**: first `services.AddScoped(typeof(IPipelineBehavior<,>), ...)` registered runs first inbound / last outbound. The canonical order (Logging → Auditing → Authorization → Validation → Transaction) and the reasoning behind it is documented in README.md - preserve that ordering rationale when adding a new behavior rather than re-deriving it.

**Result-type convention**: don't route every handler failure through a single generic `Failure`. Pick the specific `Outcome<T1, ...>` arity/case types that operation can actually produce, so the compiler forces callers to handle exactly those cases (see `documentation/messages-and-result-types.md` § "Choosing Arities").

## Conventions

- Conventional Git commits (`feat:`, `fix:`, `chore:`...), imperative mood, subject <72 chars.
- Use the newest C# .NET 11 features when appropriate, including C# 14 extension members - prefer an `extension(receiver) { }` block over classic `this`-parameter extension methods only when two or more members share the same receiver type in a static class, since that's the case where the block actually removes repetition (the receiver declared once instead of once per method); a static class with a single extension method keeps the classic `this`-parameter syntax, which stays the more widely recognized form for a one-off. `TutorialProject/` is exempt regardless of member count - it optimizes for showing widely-recognized C# to match `documentation/sample-app-walkthrough.md`, not for the newest syntax.
- One top-level public type per file, filename matches type name - except simple record/struct types, which may keep custom equality/formatting/operator members in the same file. For member ordering, partial-class splitting, generic-arity file naming, and region grouping, use the `csharp-code-organization` skill.
- Rich XML doc comments on all public members (doc-warning suppressions in `Directory.Build.props` are there so undocumented code doesn't break the build, not license to skip docs on new code).
- Value-added inline comments only - don't narrate obvious logic.
- Skill Usage
  - Use /csharp-code-organization when creating new types and organizing code
  - Use /csharp-docs-and-comments when writing and improving code
  - Use /csharp-test-sweep when writing and improving tests
  - Use /github-markdown and /mermaid-diagram-generator when writing, organizing or updating markdown documentation

### Async method naming

Any method whose return type signals asynchrony (`Task`, `Task<T>`, `ValueTask`, `ValueTask<T>`, `IAsyncEnumerable<T>`, etc.) must end in `Async`. This is enforced at build time by `VSTHRD200`, raised to `Warning` uniformly across `src/`, `TutorialProject`, and `tests/` (see `.editorconfig`) - there are no exceptions for the mediator's dispatch-contract verbs: the old unsuffixed `Handle`/`Send`/`Publish`/`Cancel` members were removed from their interfaces entirely, leaving `HandleAsync`/`SendAsync`/`PublishAsync`/`CancelAsync` as the only forms. `IBareCommandHandler<TCommand, TResult>.Handle` was the last deliberate holdout - it's now `HandleAsync` too, with no `VSTHRD200` suppression anywhere in the solution.

`[Fact]`/`[Theory]`-attributed test methods are exempt from `VSTHRD200` - verified experimentally, not just read from docs: the analyzer already excludes them with no configuration needed, since they're named for behavior, not signature. Every other async method in test code - helpers, fixtures, factories, local functions - is not exempt and must carry the `Async` suffix like everywhere else; `VSTHRD200` enforces this in `tests/` the same way it does in `src/` and `TutorialProject`.

## Agent skills

### Issue tracker

Local markdown - issues live as files under `.scratch/<feature>/`. No PR triage. See `maintainer_docs/agents/issue-tracker.md`.

### Triage labels

Default labels: `needs-triage`, `needs-info`, `ready-for-agent`, `ready-for-human`, `wontfix`. See `maintainer_docs/agents/triage-labels.md` - this also lists the terminal states (`done`, `wontfix`, `superseded`, etc.) and ADR lifecycle words (`Accepted`, `Superseded by`) that round out the accepted set. A `Status:` value outside that set on any ADR, PRD, or ticket is never written silently - see that file's "Rejecting an unrecognized status" section.

### Domain docs

Single-context: one `CONTEXT.md` and one `maintainer_docs/adr/` at the repo root. See `maintainer_docs/agents/domain.md`.

### Documentation maintenance

Docs are updated in the same iteration as the code change that makes them stale, not batched into a separate later pass. See `maintainer_docs/agents/documentation-maintenance.md`.

## Code Style Enforcement

StyleCop + Roslynator analyzers run on every build (`Directory.Build.props`); `.editorconfig` and `stylecop.json` at repo root are shared across all projects via `AdditionalFiles` linking - don't duplicate per-project style config. A specific set of StyleCop rules is suppressed solution-wide (see `NoWarn` in `Directory.Build.props`); check there before re-adding a suppression locally. `csharpier` (pinned via `dotnet-tools.json`) is the formatter - run it instead of hand-formatting.

## House Style Conventions

Every file in `documentation/` follows these conventions:

### Content Rules

- **No enumerated counts in prose**: say "a set of" / "several" and link to source instead. Exception: counts are fine when enumerating an explicit, closed set of type implementations (e.g., "the five `Outcome` failure cases: `NotFound`, `Forbidden`, `Conflict`, `ValidationError`, `Failure`").
- **Auto-generated counts are OK**: badges, CI test counts, etc., since they can't go stale.
- **Diagrams**: inline `​```mermaid` fences only. No standalone `.mmd` files - we don't have reuse-across-docs at scale yet.
- **HTTP status codes**: only `aspnetcore-integration.md` is permitted to reference HTTP status codes. All other docs link there instead.
- **Truthfulness**: every code example compiles against the shipped API. Claims are verified against `src/` code, not carried forward from older docs if they've been superseded.
- **No internal design-record citations**: never cite or link to `maintainer_docs/adr/`, `maintainer_docs/PRD.md`, `maintainer_docs/TODO.md`, or any other `maintainer_docs/` path to justify a design choice - e.g. don't write "See ADR-0009 for the no-default-pipeline design" or "`maintainer_docs/PRD.md` §9.2 has the full design." State the rationale itself, as fact, in this doc - the *why* stays, the citation to an internal decision record doesn't. `maintainer_docs/` is internal maintainer documentation (see [`maintainer_docs/README.md`](maintainer_docs/README.md)); a consumer reading this tree has no reason to open it. If a claim needs a pointer, link sideways to the sibling `documentation/` file that already covers it, never to `maintainer_docs/`.
- **Glossary linking**: the first mention of a term defined in `documentation/glossary.md` within a given file links to its glossary entry. Skip the link on later mentions in the same file, and skip it entirely when the file already explains the term inline in its own context - the glossary is for a reader who hasn't seen the term yet, not a mandatory backlink on every occurrence.

## TBD sections

No documentation sections are currently blocked on pending infrastructure work. TBD placeholders are written per the alert convention above whenever one is needed, so nothing is forgotten.
