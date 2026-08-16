# Session handoff — DI builder and discovery effort

Branch: `docs/di-builder-and-discovery-prd`, tip `2beac5e` as of the last update. Working tree clean except unrelated pre-existing `.devcontainer/*` changes (not this effort's work — left unstaged).

## Done and merged (tickets 01, 02, 03, 06, 07, 08, 09, 16 — plus one unticketed critical fix)

All merged into `docs/di-builder-and-discovery-prd`, each code-reviewed (Standards + Spec two-axis review) with findings fixed and re-verified. Full suite green at 196/196 tests.

- **01** — reconciled ADRs 0002/0003/0005/0007, added ADRs 0008–0013, corrected `CONTEXT.md` glossary.
- **02** — `Interlocutor.DependencyInjection.Abstractions` module: `InterlocutorRegistration`, `RegistrationKind`, `RegistrationLifetime`, `HandlerDiscovery`, `DefaultPipelineRegistrations`.
- **08** — `IQuery<TResponse>`/`ICommand<TResponse>` constrained to `where TResponse : IOperationOutcome`.
- **16** — pluggable `IOutcomeStatusMapper` in `Interlocutor.AspNetCore`; `Failure`→400 (was 500) with recorded rationale.
- **09** — `ISender.Send(object, CancellationToken)` runtime-typed dispatch, reflection-based; `HandlerNotRegisteredException`/`UnsupportedRequestTypeException`.
- **03** — `InterlocutorBuilder` (container-neutral fluent builder), entry points on both containers, added *alongside* legacy overloads (not yet deleted — that's ticket 04).
- **07** — `StageResult<TResponse>` short-circuit contract on `IPipelineStage`, `PipelineStageBase`, `DelegatePipelineStage`, unwind semantics in `PipelineStageBehavior`.
- **06** — `AuditingBehavior`/`AuthorizationBehavior`/both `TransactionBehavior`s constrained to their marker interfaces (`IAuditableRequest`/`IRequiresPolicy`/`ITransactionalRequest`), redundant runtime checks removed. This ticket's own new tests are what surfaced the critical fix below.
- **Unticketed critical fix** (commit `2beac5e`) — `AuthorizationBehavior` and `ValidationBehavior` both had the identical structural flaw: their generic shape (`IPipelineBehavior<TRequest, OperationOutcome<TSuccess, [Forbidden|ValidationErrors]>>`) made Microsoft.Extensions.DependencyInjection's open-generic resolver throw `ArgumentException` on *every* dispatch the instant either was registered — confirmed against a real `ServiceProvider`, not assumed. Fixed by making both generic directly over `TResponse` (matching every other built-in behavior's shape) and constructing the failure case reflectively via a new shared helper, `src/Interlocutor.Abstractions/Pipeline/UnionCaseFactory.cs`. Proven via real container resolution plus an end-to-end dispatch test through `UseRecommendedChain()`.

Review fixes worth knowing about if touching these areas again:
- `NotFound<TId>` got its own overridable `MapNotFoundGeneric` hook in `DefaultOutcomeStatusMapper` (ticket 16 review).
- `TryGetType` in `InterlocutorBuilder.cs`/`DefaultPipelineRegistrations.cs` needs assembly-qualified type name strings (`"Type, AssemblyName"`) — bare `Type.GetType(name)` can never find another assembly's type regardless of whether it's referenced (ticket 03 review). Also fixed a copy-paste arity bug there: NHibernate's `TransactionBehavior<TRequest,TResponse>` is arity 2, not EF Core's arity 3.
- `IPipelineStage.OnAfterAsync`'s `result` parameter can be `default` (not a genuine outcome) when a *later* stage's before-hook throws — documented, not changed (ticket 07 review).
- `AuthorizationBehavior<TRequest,TResponse>`/`ValidationBehavior<TRequest,TResponse>` construct their failure case via `UnionCaseFactory` (reflection over the union's compiler-generated case constructor) rather than a compile-time `TSuccess` parameter — read that type's XML remarks before changing either behavior's shape again.

## Next up — resume the ticket queue

Frontier unblocked by ticket 03 (all that remains, blocked only on 03 which is done): **04 → 05 → 15**, in that order.

- **04** (delete-legacy-registration-api) and **05** (mediator-lifetime-and-root-scope-guard) both heavily touch the DI.Microsoft/DI.Autofac registration files — don't run in parallel with each other.
- **05** and **15** (activity-tracing) likely both touch `Mediator.cs` — don't run in parallel with each other either.
- Do them sequentially: 04, then 05, then 15.

## Process notes for whoever resumes

- Tickets are implemented via parallel background `Agent` calls with `isolation: "worktree"`, each given a full ticket brief plus explicit instruction to commit incrementally (session usage limits interrupted mid-flight agents more than once this session — commit-as-you-go is what made that recoverable).
- After an agent reports done: merge its branch into `docs/di-builder-and-discovery-prd` (fast-forward when possible, `--no-ff` merge otherwise), run full `dotnet build && dotnet test`, run a two-axis `/code-review` (Standards + Spec sub-agents in parallel) against the merge, fix findings, commit, clean up the worktree (`git worktree remove`, `git branch -d`) — occasionally the worktree directory itself is left locked by a stray process and removal fails with "Device or resource busy"; harmless (git no longer tracks it), safe to leave.
- Before trusting a bug report about container-resolution or reflection behavior from a sub-agent, reproduce it directly (a throwaway console project referencing the relevant `src/*` project is usually fastest) rather than taking the agent's diagnosis on faith — that's how the critical fix above got found accurately.
- The `.scratch/improvements/issues/` backlog (separate from this effort) was checked earlier this session — tickets 01, 04, 07 there are already implemented in `src/`; nothing outstanding there.
