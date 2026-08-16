## Labels

ready-for-agent

## What to build

`InterlocutorBuilder` currently owns two distinct responsibilities in one type: assembly-scanning/handler-discovery/registration-defaults, and pipeline composition (behaviors, stages, the recommended chain). This was an open `docs/TODO.md` item ("pipeline builder pattern") — no dedicated pipeline builder exists today; `AddBehavior`, `AddStage`, and `UseRecommendedChain` all live directly on `InterlocutorBuilder` alongside `ScanAssembly`, `OverrideAuditSink`, etc.

Extract pipeline composition into its own builder type, so a consumer can reason about "what runs in what order" as a self-contained concern separate from "which assemblies get scanned" and "which infrastructure defaults are overridden."

From the developer's perspective:

- `InterlocutorBuilder`'s existing pipeline methods (`AddBehavior`, `AddLoggingBehavior`, `AddAuditingBehavior`, `AddAuthorizationBehavior`, `AddValidationBehavior`, `AddTransactionBehavior`, `UseRecommendedChain`, and the stage methods `AddStage`/`AddStageHost`) must keep compiling exactly as they do today — this is an internal restructuring, not a breaking API change. Existing consumer code (including every example in `documentation/`) must not need to change.
- Internally, these calls delegate to the new pipeline builder type rather than mutating `InterlocutorBuilder`'s own state directly.
- The pipeline builder's responsibility boundary: behaviors, stages, and the recommended-chain sugar. Discovery (`ScanAssembly*`), generated registrations, and infrastructure overrides (`OverrideAuditSink`, `OverridePolicyEvaluator`, `OverrideRequestContextAccessor`, `OverrideOutcomeDescriber`) stay on `InterlocutorBuilder`.
- `documentation/builder-and-registration.md` is updated to reflect the new internal structure where it materially changes what a consumer sees (e.g., if a new direct-access surface is added); if the public API is fully unchanged, note that explicitly rather than leaving stale prose.

## Acceptance criteria

- [ ] Pipeline composition (behaviors, stages, recommended chain) is implemented in a dedicated builder type, separate from `InterlocutorBuilder`'s discovery/defaults responsibilities
- [ ] Every existing `InterlocutorBuilder` pipeline method signature remains source-compatible — no consumer code (including `documentation/` examples) needs to change
- [ ] Existing tests for `InterlocutorBuilder`'s pipeline behavior continue to pass, plus new tests for the extracted type in isolation
- [ ] `documentation/builder-and-registration.md` reflects the change (or explicitly notes the public surface is unchanged)
- [ ] `dotnet build` and `dotnet test` pass for the whole solution

## Blocked by

None — can start immediately
