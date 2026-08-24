# ARD: Deepen the InterlocutorBuilder / InterlocutorPipelineBuilder Seam

Status: done - implemented in b483ab6 (refactor: move pipeline composition exclusively onto InterlocutorPipelineBuilder), recorded as ADR-0015

## Problem Statement

`InterlocutorBuilder` (619 lines) is the hottest churn file in the last 100 commits. Ticket 40 already
extracted pipeline-composition state into a dedicated `InterlocutorPipelineBuilder`, but every pipeline
method on `InterlocutorBuilder` - `AddBehavior`, the named `Add*Behavior` convenience methods,
`UseRecommendedChain`, the `AddStage`/`AddStageHost` family - is a one-line forward to the identically-named
method on `InterlocutorPipelineBuilder`, each carrying its own near-verbatim copy of the XML doc comment.
The split moved state sideways without deepening either side: `InterlocutorBuilder`'s interface is still
exactly as wide as `InterlocutorPipelineBuilder`'s, and the documentation burden doubled instead of
shrinking. Test coverage mirrors the duplication - `InterlocutorBuilderTests.cs` and
`InterlocutorPipelineBuilderTests.cs` both assert the same forwarding behavior (order, duplicates,
try-add precedence) at two seams.

A second, smaller inconsistency compounds this: `OverrideAuditSink`/`OverridePolicyEvaluator`/
`OverrideOutcomeDescriber` write to the shared defaults dictionary through `InterlocutorBuilder`'s own
private `SetDefault`, while `OverrideTimeoutPolicy`/`OverrideRateLimiter`/`OverrideOutcomeCaseRegistry`
already delegate through `InterlocutorPipelineBuilder`'s independently-implemented copy of the same method.
Both write to the same dictionary instance, so today's behavior is correct, but the two-writers shape is
the same shallowness surfacing as an inconsistency rather than as prose duplication.

## Solution

`InterlocutorBuilder.Pipeline` (new, get-only, returns `InterlocutorPipelineBuilder`) becomes the sole
entry point for pipeline composition. `InterlocutorBuilder` drops every mutating pipeline method it
currently forwards. `InterlocutorPipelineBuilder` becomes the one deep module for "what runs in what
order," with the one copy of the doc comment for each method, and the one canonical write path to the
shared defaults dictionary.

This is a deliberate breaking change to the public builder surface - consistent with this project's
existing pre-1.0, no-shims norm (see ticket 04, which deleted the legacy registration API outright rather
than deprecating it forward). It reverses ticket 40's explicit "the public surface shown above is
unchanged" promise, recorded today only in a `documentation/builder-and-registration.md` `[!NOTE]`
callout and in code comments - never in an ADR. A new ADR records this reversal and why.

## Final Shape

**`InterlocutorBuilder` keeps, unchanged:**

- `ScanAssembly` / `ScanAssemblies` / `ScanAssemblyContaining<TMarker>` / `ScanEntryAssembly`
- `AddGeneratedRegistrations`, `AssembliesToScan`, `GeneratedRegistrations`
- `Behaviors`, `Stages`, `StageFactories` - stay as pass-through properties (see rationale below)
- `Defaults` - already correctly aggregates the shared dictionary regardless of which class wrote to it;
  no change needed
- `OverrideRequestContextAccessor` - the one genuinely ambient default, seeded in the constructor
  independent of any pipeline behavior

**`InterlocutorBuilder` gains:**

- `Pipeline` property (get-only, `InterlocutorPipelineBuilder`)

**`InterlocutorBuilder` loses** (moves to `InterlocutorPipelineBuilder`, reachable only via `.Pipeline`):

- `AddBehavior`
- `AddLoggingBehavior`, `AddAuditingBehavior`, `AddAuthorizationBehavior`, `AddValidationBehavior`,
  `AddTransactionBehavior`, `AddCancellationBehavior`, `AddTimeoutBehavior`, `AddRateLimitedBehavior`
- `UseRecommendedChain`
- `AddStage` (all four overloads), `AddStageHost`
- `OverrideAuditSink`, `OverridePolicyEvaluator`, `OverrideOutcomeDescriber` - joining
  `OverrideTimeoutPolicy`, `OverrideRateLimiter`, `OverrideOutcomeCaseRegistry`, which already live on
  `InterlocutorPipelineBuilder` today. All six override a dependency of a pipeline behavior, so "which
  service is a pipeline-behavior dependency" and "which class owns overriding it" become the same
  question.

**Defaults write path:** `InterlocutorPipelineBuilder.SetDefault` becomes the one canonical writer
(`internal`), called by `InterlocutorBuilder.OverrideRequestContextAccessor` too. Delete
`InterlocutorBuilder`'s own private `SetDefault` copy.

**Why `Behaviors`/`Stages`/`StageFactories` stay on `InterlocutorBuilder` as pass-throughs:** they are
already single-line forwards, consumed directly by `ServiceCollectionExtensions.cs` (Microsoft) and
`ContainerBuilderExtensions.cs` (Autofac). They were never the duplication cost this ARD targets (that
was the 17 mutating methods' duplicated prose) - moving them buys no depth and costs edits in two more
packages plus their test projects.

## Blast Radius (verified against the codebase)

Source: `src/Interlocutor.DependencyInjection.Abstractions/InterlocutorBuilder.cs` and
`InterlocutorPipelineBuilder.cs` only - no other `src/` file calls these methods.

Call sites needing mechanical `builder.X()` → `builder.Pipeline.X()` updates:

- `tests/Interlocutor.DependencyInjection.Abstractions.Tests/InterlocutorBuilderTests.cs` - also trimmed
  to 1-2 smoke assertions (`.Pipeline` non-null, stable reference across calls); full behavioral matrix
  (order, duplicates, try-add precedence) stays solely in `InterlocutorPipelineBuilderTests.cs`
- `tests/Interlocutor.DependencyInjection.Abstractions.Tests/InterlocutorPipelineBuilderTests.cs`
- `tests/Interlocutor.DependencyInjection.Autofac.Tests/BehaviorSubsetConstraintTests.cs`
- `tests/Interlocutor.DependencyInjection.Autofac.Tests/ContainerBuilderExtensionsTests.cs`
- `tests/Interlocutor.DependencyInjection.Autofac.Tests/InterlocutorBuilderIntegrationTests.cs`
- `tests/Interlocutor.DependencyInjection.Autofac.Tests/RootScopeGuardTests.cs`
- `tests/Interlocutor.DependencyInjection.Microsoft.Tests/BehaviorSubsetConstraintTests.cs`
- `tests/Interlocutor.DependencyInjection.Microsoft.Tests/GeneratedRegistrationIntegrationTests.cs`
- `tests/Interlocutor.DependencyInjection.Microsoft.Tests/InterlocutorBuilderIntegrationTests.cs`
- `tests/Interlocutor.DependencyInjection.Microsoft.Tests/PipelineOrderingIntegrationTests.cs`
- `tests/Interlocutor.DependencyInjection.Microsoft.Tests/RootScopeGuardTests.cs`
- `tests/Interlocutor.DependencyInjection.Microsoft.Tests/ServiceCollectionExtensionsTests.cs`

Documentation needing the same call-shape update, plus removal/rewrite of the now-false
`builder-and-registration.md` `[!NOTE]`:

- `README.md`
- `documentation/builder-and-registration.md`
- `documentation/mediator-lifetimes-and-scope.md`
- `documentation/pipeline-and-behaviors.md`
- `documentation/transactions-and-store-integration.md`
- `documentation/testing.md`
- `documentation/glossary.md` - also gains a new **InterlocutorPipelineBuilder** entry under "DI
  Registration" (it was never added when ticket 40 shipped, even though the type has existed since then)

## Acceptance Criteria

- [ ] `InterlocutorBuilder` exposes no mutating pipeline method - only `Pipeline`, the `Scan*` family,
      `AddGeneratedRegistrations`, `OverrideRequestContextAccessor`, and the `Behaviors`/`Stages`/
      `StageFactories`/`Defaults`/`GeneratedRegistrations`/`AssembliesToScan` read-only projections
- [ ] `InterlocutorPipelineBuilder` owns all six behavior-dependency `Override*` methods
- [ ] Exactly one write path to the shared defaults dictionary (`InterlocutorPipelineBuilder.SetDefault`,
      internal); `InterlocutorBuilder`'s own copy is deleted
- [ ] Every existing behavioral test passes unmodified in substance, only updated for the new call shape
      (`builder.Pipeline.X()` instead of `builder.X()`)
- [ ] `InterlocutorBuilderTests.cs` no longer duplicates `InterlocutorPipelineBuilderTests.cs`'s
      behavioral assertions - only smoke-tests `.Pipeline`'s presence/stability
- [ ] `README.md` and all six listed `documentation/` files reflect `.Pipeline`; the ticket-40 `[!NOTE]`
      claiming an unchanged public surface is removed or rewritten to point at the new ADR
- [ ] `documentation/glossary.md` has an `InterlocutorPipelineBuilder` entry
- [ ] A new ADR (next number after 0014) records this decision and explicitly supersedes/amends ticket
      40's compatibility promise
- [ ] `dotnet build` and `dotnet test` are clean across the whole solution

## Out of Scope

- Moving `Behaviors`/`Stages`/`StageFactories`/`Defaults`/`GeneratedRegistrations` behind `.Pipeline` -
  explicitly rejected; see rationale above
- Any change to `ServiceCollectionExtensions.cs` or `ContainerBuilderExtensions.cs` - they keep reading
  `builder.Behaviors`/`.Stages`/`.StageFactories`/`.Defaults` exactly as today
- Any change to `AssembliesToScan`, `GeneratedRegistrations`, or the `Scan*` methods - discovery is a
  separate concern, untouched by this ARD
- Candidates 2 (generate the per-arity `ResultExtensions` overloads) and 3 (trim ADR-0014's unbuilt
  narrative out of `InterlocutorBuilder`'s doc comment) from the architecture review this ARD came from -
  tracked separately if pursued

## Ticketing Note

This ARD was deliberately kept as one candidate rather than broken into tracer-bullet tickets: the change
is mechanical (a rename-and-move, not new behavior), fits in one atomic commit that keeps the whole
solution green (not a multi-repo or thousands-of-call-sites wide refactor needing expand/contract
staging), and splitting the code change from its documentation/test updates was explicitly rejected during
design (an API reshape with stale docs or redundant tests behind it is exactly the drift this effort
exists to prevent). Break it into tickets with `/to-tickets` when ready to schedule the work; the
Acceptance Criteria above are written to hand off directly as a single ticket's criteria if no further
breakdown turns out to be needed.
