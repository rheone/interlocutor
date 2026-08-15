# Session handoff — DI builder and discovery effort

Written mid-session, paused per user request. Branch: `docs/di-builder-and-discovery-prd`, tip `14d9e4a` at time of writing. Working tree clean, no worktrees or stray branches left behind.

## Done and merged (tickets 01, 02, 03, 06, 07, 08, 09, 16)

All merged into `docs/di-builder-and-discovery-prd`, each code-reviewed (Standards + Spec two-axis review) with findings fixed and re-verified. Full suite green at 192/192 tests as of the last full run, before the blocker below was found.

- **01** — reconciled ADRs 0002/0003/0005/0007, added ADRs 0008–0013, corrected `CONTEXT.md` glossary.
- **02** — `Interlocutor.DependencyInjection.Abstractions` module: `InterlocutorRegistration`, `RegistrationKind`, `RegistrationLifetime`, `HandlerDiscovery`, `DefaultPipelineRegistrations`.
- **08** — `IQuery<TResponse>`/`ICommand<TResponse>` constrained to `where TResponse : IOperationOutcome`.
- **16** — pluggable `IOutcomeStatusMapper` in `Interlocutor.AspNetCore`; `Failure`→400 (was 500) with recorded rationale.
- **09** — `ISender.Send(object, CancellationToken)` runtime-typed dispatch, reflection-based; `HandlerNotRegisteredException`/`UnsupportedRequestTypeException`.
- **03** — `InterlocutorBuilder` (container-neutral fluent builder), entry points on both containers, added *alongside* legacy overloads (not yet deleted — that's ticket 04).
- **07** — `StageResult<TResponse>` short-circuit contract on `IPipelineStage`, `PipelineStageBase`, `DelegatePipelineStage`, unwind semantics in `PipelineStageBehavior`.
- **06** — `AuditingBehavior`/`AuthorizationBehavior`/both `TransactionBehavior`s constrained to their marker interfaces (`IAuditableRequest`/`IRequiresPolicy`/`ITransactionalRequest`), redundant runtime checks removed. **This ticket's own new tests are what surfaced the blocker below.**

Each merge commit and review-fix commit is on the branch; `git log --oneline` tells the story in order. Review fixes worth knowing about if touching these areas again:
- `NotFound<TId>` got its own overridable `MapNotFoundGeneric` hook in `DefaultOutcomeStatusMapper` (ticket 16 review).
- `TryGetType` in `InterlocutorBuilder.cs`/`DefaultPipelineRegistrations.cs` needed assembly-qualified type name strings (`"Type, AssemblyName"`) — bare `Type.GetType(name)` can never find another assembly's type regardless of whether it's referenced (ticket 03 review). Also fixed a copy-paste arity bug: NHibernate's `TransactionBehavior<TRequest,TResponse>` is arity 2, not EF Core's arity 3.
- `IPipelineStage.OnAfterAsync`'s `result` parameter can be `default` (not a genuine outcome) when a *later* stage's before-hook throws — documented, not changed (ticket 07 review).

## BLOCKED — critical bug found, fix in progress, not yet started

**`AuthorizationBehavior<TRequest,TSuccess>` cannot be resolved via Microsoft.Extensions.DependencyInjection's open-generic support, at all, for any input.** Confirmed with a standalone repro (not checked in — was in a `/tmp` scratch dir, already deleted): `services.GetServices<IPipelineBehavior<TRequest,TResponse>>()` — exactly what `Mediator.cs` calls on every dispatch — throws `ArgumentException` the instant `AuthorizationBehavior<,>` is registered as an open generic, regardless of whether the request implements `IRequiresPolicy`.

**Root cause:** `AuthorizationBehavior<TRequest, TSuccess> : IPipelineBehavior<TRequest, OperationOutcome<TSuccess, Forbidden>>` — the implementation's second type parameter (`TSuccess`) is *nested inside* the interface's second type argument, not mapped to it 1:1. Every other built-in behavior (`LoggingBehavior<TRequest,TResponse> : IPipelineBehavior<TRequest,TResponse>`, etc.) maps its type parameters directly onto the interface's type arguments, which is the only shape Microsoft DI's open-generic resolver supports — it does naive positional substitution (`implementationType.MakeGenericType(serviceType.GetGenericArguments())`), not real generic unification. Autofac's `RegisterGeneric` does real structural unification and handles the nested shape fine, which is why this was never caught: every prior test (ticket 03 onward) only checked that `AuthorizationBehavior` was *registered* (`IServiceCollection` contents), never that it actually *resolved* through a built `ServiceProvider`. Ticket 06's new tests are the first to actually try resolving it, and immediately hit this — the ticket-06 agent correctly found and documented it but (rightly, given ticket 06's scope) didn't fix it, working around it with an Autofac-only proof and a reflection-based `MakeGenericType` check on the Microsoft side.

**Blast radius:** the moment a Microsoft DI consumer calls `.AddAuthorizationBehavior()` or `.UseRecommendedChain()`, *every* request dispatched through the mediator crashes — not just policy-gated ones. Launch-blocking for one of the two supported containers.

**User's decision (asked via AskUserQuestion): fix now, before continuing the ticket queue.**

### Fix direction (not yet implemented — was investigating when paused)

The shape needs to change so `AuthorizationBehavior`'s type parameters map 1:1 onto `IPipelineBehavior<TRequest,TResponse>` like every other behavior — i.e. `AuthorizationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : IRequiresPolicy where TResponse : IOperationOutcome`. The remaining problem: today the behavior short-circuits by constructing `new OperationOutcome<TSuccess, Forbidden>(new Forbidden(...))` directly, using the compile-time `TSuccess` type parameter and the union's implicit `Forbidden → OperationOutcome<TSuccess,Forbidden>` conversion. If `TResponse` is genericized directly (no longer decomposed into `TSuccess`), there's no compile-time way to know `TResponse` even *has* a `Forbidden` case or how to construct it.

Was mid-investigation into whether `src/Interlocutor.Abstractions/Pipeline/Classifiers/{IOutcomeClassifier,OutcomeClassifierBuilder}.cs` already has a reusable mechanism for this kind of generic union-case construction/inspection (they exist for describing/classifying *existing* outcome instances, e.g. for `TransactionBehavior`/`LoggingBehavior`'s commit-vs-rollback decision — hadn't yet confirmed whether they also handle *constructing* a specific case generically, which is the harder direction). Had not yet read those two files' contents at pause time.

**Leading candidate approach** (needs validation against what the classifier module already does, and against existing tests/consumers of `AuthorizationBehavior`): reflectively invoke `TResponse`'s compiler-generated implicit conversion operator from `Forbidden` (`op_Implicit`) to construct the short-circuit value at runtime, once per closed generic (cache the `MethodInfo`, same pattern as ticket 09's `ResolveVoidCommandResultType`/`InvokeSendAsync`). This keeps the *outward* shape identical to every other behavior (fixing the Microsoft DI bug) while preserving the runtime behavior. Alternative approaches (a `static abstract` interface member on a hypothetical `IHasForbiddenCase<TSelf>` that `OperationOutcome<TSuccess,Forbidden>` would need to implement) look architecturally heavier and touch the C#15 union declarations in `Interlocutor.CommonKernel`, which is likely more disruptive than necessary — hadn't ruled this out, just deprioritized it pending the classifier-module read.

### Next steps to resume

1. Read `src/Interlocutor.Abstractions/Pipeline/Classifiers/IOutcomeClassifier.cs` and `OutcomeClassifierBuilder.cs` in full — check for a reusable generic-case-construction pattern before building a new one.
2. Read the current `src/Interlocutor.Abstractions/Pipeline/AuthorizationBehavior.cs` and its tests (`tests/Interlocutor.Abstractions.Tests/AuthorizationBehaviorTests.cs`) in full to understand exactly what must keep working.
3. Design and implement the fix (TDD — this is exactly the kind of behavioral seam TDD suits: given a request implementing `IRequiresPolicy` whose policy fails, the behavior must short-circuit with a `Forbidden`-shaped case of whatever `TResponse` the caller declared).
4. **Prove the fix with a real container-resolution test on Microsoft DI** — build an actual `ServiceProvider` and call `GetServices<IPipelineBehavior<TRequest,TResponse>>()` (not `GetService`, not a registration-list check) for a request implementing `IRequiresPolicy`, and confirm it resolves without throwing and behaves correctly. This is the test category that was missing before and let this bug hide — don't repeat that gap.
5. Also re-verify the reflection-based Microsoft DI test ticket 06 added (`BehaviorSubsetConstraintTests.cs` in the Microsoft test project) — it currently works around the bug via `MakeGenericType` reflection rather than real resolution; once the shape is fixed, that test should probably be upgraded to real resolution too, matching the Autofac side.
6. Run full solution build + test, run `/code-review` (Standards + Spec sub-agents) on the fix, apply any findings, commit.
7. **Then resume the ticket queue**: 04 (delete-legacy-registration-api) → 05 (mediator-lifetime-and-root-scope-guard) → 15 (activity-tracing), in that order — 04 and 05 both heavily touch the DI.Microsoft/DI.Autofac registration files so shouldn't run in parallel with each other; 05 and 15 likely both touch `Mediator.cs` so shouldn't run in parallel either. 06's already-merged marker-constraint work and this `AuthorizationBehavior` fix are both prerequisites those three don't otherwise depend on, but doing this fix first avoids compounding the bug under more layers.

## Process notes for whoever resumes

- Tickets are being implemented via parallel background `Agent` calls with `isolation: "worktree"`, each given a full ticket brief plus explicit instruction to commit incrementally (session usage limits have interrupted mid-flight agents at least once this session — commit-as-you-go is what made that recoverable).
- After an agent reports done, merge its branch into `docs/di-builder-and-discovery-prd` (fast-forward when possible, `--no-ff` merge otherwise), run full `dotnet build && dotnet test`, then run a two-axis `/code-review` (Standards + Spec sub-agents in parallel) against the merge, fix findings, commit, clean up the worktree (`git worktree remove`, `git branch -d`) — occasionally the worktree directory itself is left locked by a stray process and `rm -rf`/`git worktree remove` fails with "Device or resource busy"; this is harmless (git no longer tracks it) and safe to leave.
- The `.scratch/improvements/issues/` backlog (separate from this effort) was checked earlier this session — tickets 01, 04, 07 there are already implemented in `src/`; nothing outstanding there.
