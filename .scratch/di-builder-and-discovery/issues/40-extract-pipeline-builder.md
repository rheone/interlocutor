## Labels

ready-for-agent

## What to build

`InterlocutorBuilder` currently owns two distinct responsibilities in one type: assembly-scanning/handler-discovery/registration-defaults, and pipeline composition (behaviors, stages, the recommended chain). This was an open `docs/TODO.md` item ("pipeline builder pattern") - no dedicated pipeline builder exists today; `AddBehavior`, `AddStage`, and `UseRecommendedChain` all live directly on `InterlocutorBuilder` alongside `ScanAssembly`, `OverrideAuditSink`, etc.

Extract pipeline composition into its own builder type, so a consumer can reason about "what runs in what order" as a self-contained concern separate from "which assemblies get scanned" and "which infrastructure defaults are overridden."

From the developer's perspective:

- `InterlocutorBuilder`'s existing pipeline methods (`AddBehavior`, `AddLoggingBehavior`, `AddAuditingBehavior`, `AddAuthorizationBehavior`, `AddValidationBehavior`, `AddTransactionBehavior`, `UseRecommendedChain`, and the stage methods `AddStage`/`AddStageHost`) must keep compiling exactly as they do today - this is an internal restructuring, not a breaking API change. Existing consumer code (including every example in `documentation/`) must not need to change.
- Internally, these calls delegate to the new pipeline builder type rather than mutating `InterlocutorBuilder`'s own state directly.
- The pipeline builder's responsibility boundary: behaviors, stages, and the recommended-chain sugar. Discovery (`ScanAssembly*`), generated registrations, and infrastructure overrides (`OverrideAuditSink`, `OverridePolicyEvaluator`, `OverrideRequestContextAccessor`, `OverrideOutcomeDescriber`) stay on `InterlocutorBuilder`.
- `documentation/builder-and-registration.md` is updated to reflect the new internal structure where it materially changes what a consumer sees (e.g., if a new direct-access surface is added); if the public API is fully unchanged, note that explicitly rather than leaving stale prose.

## Acceptance criteria

- [x] Pipeline composition (behaviors, stages, recommended chain) is implemented in a dedicated builder type, separate from `InterlocutorBuilder`'s discovery/defaults responsibilities
- [x] Every existing `InterlocutorBuilder` pipeline method signature remains source-compatible - no consumer code (including `documentation/` examples) needs to change
- [x] Existing tests for `InterlocutorBuilder`'s pipeline behavior continue to pass, plus new tests for the extracted type in isolation
- [x] `documentation/builder-and-registration.md` reflects the change (or explicitly notes the public surface is unchanged)
- [x] `dotnet build` and `dotnet test` pass for the whole solution

## Notes

- The new type is `Interlocutor.DependencyInjection.Abstractions.InterlocutorPipelineBuilder` (`src/Interlocutor.DependencyInjection.Abstractions/InterlocutorPipelineBuilder.cs`). It owns the actual `_behaviors`/`_stages`/`_stageFactories` list state; `InterlocutorBuilder` holds a private `InterlocutorPipelineBuilder` instance and every pipeline method on `InterlocutorBuilder` is now a thin wrapper delegating to it.
- **Ticket drift resolved**: by the time this ticket was picked up, `InterlocutorBuilder` had grown three more pipeline methods beyond the ticket's original enumeration - `AddCancellationBehavior`, `AddTimeoutBehavior`, `AddRateLimitedBehavior` - plus their `Override*` siblings `OverrideTimeoutPolicy`, `OverrideRateLimiter`, `OverrideOutcomeCaseRegistry`. All three `Add*Behavior` methods moved to `InterlocutorPipelineBuilder` alongside the rest (same treatment as the ticket's original six). The three `Override*` methods were judged to belong on `InterlocutorPipelineBuilder` too, not `InterlocutorBuilder`: unlike `OverrideAuditSink`/`OverridePolicyEvaluator`/`OverrideRequestContextAccessor`/`OverrideOutcomeDescriber` (which override infrastructure defaults used broadly - request context, logging/auditing description), `OverrideTimeoutPolicy`/`OverrideRateLimiter`/`OverrideOutcomeCaseRegistry` each override a dependency of one specific pipeline behavior (`TimeoutBehavior`, `RateLimitedBehavior`, and the outcome-case-registry consumers respectively), so they read as pipeline concerns, not general infrastructure. `InterlocutorBuilder`'s own copies of these three methods still exist and keep compiling exactly as before - they now delegate to the pipeline builder's versions.
- The two builders share one `Dictionary<Type, InterlocutorRegistration>` defaults instance (constructed by `InterlocutorBuilder`, passed to `InterlocutorPipelineBuilder` via an `internal` constructor) so that a pipeline default an `Add*Behavior` call try-adds and an infrastructure `Override*` call on `InterlocutorBuilder` targeting the same service type are always the same dictionary entry, regardless of call order - preserving the existing "override wins regardless of order" tests without change.
- New isolated tests: `tests/Interlocutor.DependencyInjection.Abstractions.Tests/InterlocutorPipelineBuilderTests.cs`, constructing `InterlocutorPipelineBuilder` directly (not through `InterlocutorBuilder`).

## Blocked by

None - can start immediately
