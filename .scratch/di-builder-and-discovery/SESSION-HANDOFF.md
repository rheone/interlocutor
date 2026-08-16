# Session handoff - DI builder and discovery effort

Branch: `docs/di-builder-and-discovery-prd`, tip `117daf9`, **pushed to origin** as of this update. Working tree clean except unrelated pre-existing `.devcontainer/*` changes (not this effort's work - left unstaged, never commit them as part of this effort).

## Done and merged (tickets 01–09, 15, 16 - plus one unticketed critical fix)

All merged into `docs/di-builder-and-discovery-prd`, each code-reviewed (Standards + Spec two-axis review) with findings fixed and re-verified. Full suite green at **217/217 tests**.

- **01** - reconciled ADRs 0002/0003/0005/0007, added ADRs 0008–0013, corrected `CONTEXT.md` glossary.
- **02** - `Interlocutor.DependencyInjection.Abstractions` module: `InterlocutorRegistration`, `RegistrationKind`, `RegistrationLifetime`, `HandlerDiscovery`, `DefaultPipelineRegistrations`.
- **08** - `IQuery<TResponse>`/`ICommand<TResponse>` constrained to `where TResponse : IOperationOutcome`.
- **16** - pluggable `IOutcomeStatusMapper` in `Interlocutor.AspNetCore`; `Failure`→400 (was 500) with recorded rationale.
- **09** - `ISender.Send(object, CancellationToken)` runtime-typed dispatch, reflection-based; `HandlerNotRegisteredException`/`UnsupportedRequestTypeException`.
- **03** - `InterlocutorBuilder` (container-neutral fluent builder), entry points on both containers.
- **07** - `StageResult<TResponse>` short-circuit contract on `IPipelineStage`, `PipelineStageBase`, `DelegatePipelineStage`, unwind semantics in `PipelineStageBehavior`.
- **06** - `AuditingBehavior`/`AuthorizationBehavior`/both `TransactionBehavior`s constrained to their marker interfaces. This ticket's own new tests are what surfaced the critical fix below.
- **04** - deleted both `InterlocutorOptions` types and every legacy `AddInterlocutor` overload (including the entry-assembly convenience form and the `EnableDefaultBehaviors` flag) from both DI packages. Each package now exposes exactly one `AddInterlocutor(Action<InterlocutorBuilder>)` entry point. Migrated tests preserved intent, none deleted.
- **05** - root-scope guard (ADR-0011): `IRootScopeDetector` (container-neutral, in `Interlocutor.DependencyInjection.Abstractions`), `RootScopeGuardedMediator` decorator, `MediatorResolvedFromRootScopeException`, per-container detectors (`MicrosoftRootScopeDetector` via a captured-root-`IServiceProvider` marker, `AutofacRootScopeDetector` via `ILifetimeScope.Tag` vs `LifetimeScope.RootTag`). Mediator itself never creates its own scope. This ticket's merge also fixed several ticket-03-era integration tests that were dispatching directly from the root provider/container (the exact misuse case) - they now resolve from a real scope.
- **15** - `MediatorActivitySource` (`Interlocutor.Abstractions.Diagnostics`, name `"Interlocutor.Mediator"`, public const). Every `Mediator.Send`/`Publish` overload (including ticket-09's runtime-typed `Send(object,...)`) traces via `System.Diagnostics.Activity`, tagging request type/handler type/outcome case; failure outcomes don't set Error status, thrown exceptions do; zero-cost when unlistened (verified, not assumed); coexists with the independent opt-in `LoggingBehavior` (now covered by an explicit test, not just "true by construction").
- **Unticketed critical fix** (commit `2beac5e`) - `AuthorizationBehavior` and `ValidationBehavior` both had the identical structural flaw: their generic shape (`IPipelineBehavior<TRequest, OperationOutcome<TSuccess, [Forbidden|ValidationErrors]>>`) made Microsoft.Extensions.DependencyInjection's open-generic resolver throw `ArgumentException` on *every* dispatch the instant either was registered - confirmed against a real `ServiceProvider`, not assumed. Fixed by making both generic directly over `TResponse` and constructing the failure case reflectively via a new shared helper, `src/Interlocutor.Abstractions/Pipeline/UnionCaseFactory.cs`.

Review fixes worth knowing about if touching these areas again:
- `NotFound<TId>` got its own overridable `MapNotFoundGeneric` hook in `DefaultOutcomeStatusMapper` (ticket 16 review).
- `TryGetType` in `InterlocutorBuilder.cs`/`DefaultPipelineRegistrations.cs` needs assembly-qualified type name strings (`"Type, AssemblyName"`) - bare `Type.GetType(name)` can never find another assembly's type regardless of whether it's referenced (ticket 03 review). Also fixed a copy-paste arity bug there: NHibernate's `TransactionBehavior<TRequest,TResponse>` is arity 2, not EF Core's arity 3.
- `IPipelineStage.OnAfterAsync`'s `result` parameter can be `default` (not a genuine outcome) when a *later* stage's before-hook throws - documented, not changed (ticket 07 review).
- `AuthorizationBehavior<TRequest,TResponse>`/`ValidationBehavior<TRequest,TResponse>` construct their failure case via `UnionCaseFactory` (reflection over the union's compiler-generated case constructor) rather than a compile-time `TSuccess` parameter - read that type's XML remarks before changing either behavior's shape again.
- `RootScopeGuardedMediator` wraps the real `Mediator` - `IMediator`/`ISender`/`IPublisher` all alias to the guarded decorator now, not the raw `Mediator`, in both containers' registration. Every DI integration test that dispatches must resolve from a real scope (`provider.CreateScope()`/`container.BeginLifetimeScope()`), never the root - several pre-existing tests needed that fix during ticket 05's merge.
- `Mediator.cs` grew tracing logic (ticket 15) on top of the existing reflection-cache dispatch machinery (tickets 09/05) - it's a genuinely dense file now; read it in full before touching dispatch again, don't assume you remember its shape from an earlier ticket's summary.

## DONE, NOT YET MERGED - ticket 10 (generator-foundation)

**The background agent finished** (reported complete after this note was first written) **but its work has deliberately NOT been merged, tested, or reviewed yet** - the session was stopped before that could happen. Its branch (see below) still exists untouched; treat this exactly like a freshly-completed agent and run the standard routine before trusting any of it.

**Agent's own branch:** `ticket-10-generator-foundation`, created off `117daf9` after it found its assigned worktree stale. Its own worktree (`.claude/worktrees/agent-aceeec9786105d65b`) may still be present - check `git worktree list` and clean it up (unlock/remove/prune, delete both `worktree-agent-aceeec9786105d65b` and `ticket-10-generator-foundation` branches) only *after* merging, per the usual routine.

**Agent's self-reported summary** (unverified - confirm all of this yourself before trusting it):
- Commits `41a0207`, `b07fcd7`, `5fad783` on top of `117daf9`.
- Claims 230/230 tests passing (11 test projects, including new `Interlocutor.Generator.Tests`), 0 build errors.
- New project `src/Interlocutor.Generator/` (netstandard2.0 `IIncrementalGenerator`, `Microsoft.CodeAnalysis.CSharp` 4.14.0 + `Microsoft.CodeAnalysis.Analyzers` 3.11.0, newly pinned in `Directory.Packages.props`). Test project `tests/Interlocutor.Generator.Tests/` drives `CSharpGeneratorDriver` directly rather than the Roslyn testing package (says that package pulls xunit v2, conflicting with this repo's xunit.v3-only convention - worth independently confirming this reasoning holds).
- Emitted registration module: `Interlocutor.Generated.InterlocutorGeneratedRegistrations` (public static class, `Registrations` property, built from `typeof(...)` tokens, no reflection). New assembly-level attribute `InterlocutorGeneratedModuleAttribute(Type moduleType)` in `Interlocutor.DependencyInjection.Abstractions` - the ticket-12 aggregation hook. `InterlocutorBuilder` gained `AddGeneratedRegistrations(...)`, applied by both containers *alongside* (not replacing) `HandlerDiscovery.Discover`.
- Diagnostic `INTERLOC001`, category `Interlocutor.Discovery`, default `Warning`, escalatable via `.editorconfig`'s `dotnet_diagnostic.INTERLOC001.severity = error`.
- Closed dispatch wrappers resolve via `IServiceProvider.GetService(Type)` only (no `Activator.CreateInstance`/`MakeGenericType`/`new Handler(...)`) - claims verified by a real constructor-injection integration test in both DI test projects. **Explicitly did NOT wire these into `Mediator.cs`'s dispatch path** - judged that real cross-assembly discovery/dispatch-table work belongs to ticket 12, not this one; the wrappers exist and are tested but aren't consumed by `Mediator.Send` yet. This was an explicit, reasoned scope call the agent made itself - worth scrutinizing in review rather than assuming it's correct.
- Reports fixing two real bugs found during its own integration testing: private/nested handler types producing `CS0122` in generated `typeof()` references (now excluded by `HandlerTypeAnalyzer`), and invalid `<see cref>` XML in generated doc comments for generic types (now escaped).

**Before doing anything else with this**, the standard routine applies (see Process notes below) - merge, full `dotnet build && dotnet test` yourself (don't just trust the "230/230" claim), two-axis `/code-review` (Standards + Spec sub-agents), fix findings, commit, clean up the worktree. Given the size and the self-reported scope-narrowing decision (no `Mediator.cs` wiring), budget real time for the review pass and treat every claim above as something to verify, not something already confirmed.

## After ticket 10 lands - remaining queue

Per `.scratch/di-builder-and-discovery/issues/*.md`'s `Blocked by` sections, checked fresh as of this session:

- **11** (open-generic-handlers) - blocked by 10.
- **12** (cross-assembly-modules) - blocked by 10.
- **13** (generated-dispatch-switch) - blocked by 09 (done).
- **14** (notification-fan-out) - blocked by 10.
- **17** (trim-and-aot-hardening) - blocked by 12.
- **18** (sample-application) - blocked by 04 (done), 07 (done), **14** (not done).
- **19** (readme-and-diagrams) - blocked by 17.

So once 10 lands: **11, 12, 13, 14 all become unblocked simultaneously.** Check file overlap carefully before parallelizing - 11/12 both extend the generator itself (likely overlapping files in the new generator project), 13 extends `Mediator.cs`'s runtime-typed dispatch from ticket 09 (may overlap with whatever ticket 10 did to `Mediator.cs`), 14 touches notification dispatch (`NotificationHandlerWrapper` family, which ticket 15 already touched for tracing - re-read those files fresh, don't assume their shape). Given this session's file-overlap mistakes were the main source of merge friction, err toward sequencing over parallelizing here unless you've actually diffed what each ticket touches.

## Process notes for whoever resumes

- Tickets are implemented via parallel/sequential background `Agent` calls with `isolation: "worktree"`, each given a full ticket brief plus explicit instruction to commit incrementally (session usage limits interrupted mid-flight agents more than once this session - commit-as-you-go is what made that recoverable).
- **Worktree corruption is a recurring, not one-off, problem this session** - the `Agent` tool has repeatedly refused to create a worktree with "git resolves its working tree to ... a core.worktree redirect" errors, and separately, agents have repeatedly been checked out on a stale/unrelated branch (`a1672d1`, an ancient commit) instead of the current tip. Fix for the first: `git worktree unlock <path>`, `git worktree remove --force <path>`, `git branch -D <stale-branch>`, then retry the `Agent` call. Fix for the second: tell the agent in its own prompt to verify its branch/base commit itself and self-correct by creating a fresh local branch off a known-good SHA if it's on the wrong one (this has worked reliably every time it's been tried).
- After an agent reports done: merge its branch into `docs/di-builder-and-discovery-prd` (fast-forward when possible, `--no-ff` merge otherwise), run full `dotnet build && dotnet test`, run a two-axis `/code-review` (Standards + Spec sub-agents in parallel) against the merge, fix findings, commit, clean up the worktree (`git worktree remove`, `git branch -d` - including deleting BOTH the `worktree-agent-<id>` branch the tool created AND any differently-named branch the agent self-corrected onto, e.g. `ticket-NN-<slug>`), push to origin when the user asks.
- Occasionally the worktree directory itself is left locked by a stray process and removal fails with "Device or resource busy" / "Permission denied" - harmless (git no longer tracks it once `git worktree remove`/`prune` succeeds at the metadata level), safe to leave the directory behind.
- Before trusting a bug report about container-resolution or reflection behavior from a sub-agent, reproduce it directly (a throwaway console project referencing the relevant `src/*` project, in the scratchpad temp dir, is usually fastest) rather than taking the agent's diagnosis on faith - that's how the critical AuthorizationBehavior/ValidationBehavior fix got found accurately, and how its actual fix mechanism (constructor-per-case-type from the C#15 union feature, not `op_Implicit` as first guessed) was discovered.
- The `.scratch/improvements/issues/` backlog (separate from this effort) was checked earlier this session - tickets 01, 04, 07 there are already implemented in `src/`; nothing outstanding there.
