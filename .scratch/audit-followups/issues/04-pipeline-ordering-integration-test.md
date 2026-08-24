## Labels

ready-for-agent

## What to build

Add an end-to-end test that wires the real Logging, Auditing, Authorization, Validation, and Transaction behaviors together in the documented canonical order and asserts the observed execution order via an actual `Send`, not just a list of registered behavior types.

No test anywhere registers all five real behaviors together and records the order they actually execute in - registration-list-level order checks exist, but none run an actual request through the composed pipeline.

> **Update (2026-08-18):** the ticket originally cited `DefaultPipelineRegistrationsTests.cs::Behaviors_PreservesCanonicalOrder_LoggingThenAuditingThenAuthorization` as the closest existing (incomplete) coverage. That file and the orphaned `DefaultPipelineRegistrations.cs` catalog it tested were deleted in `audit-followups/10` (confirmed unused by either real `AddInterlocutor()` registration path). The "builder's default registration path" below means `InterlocutorBuilder.UseRecommendedChain()` - the real, wired path - not that deleted class. The last acceptance criterion is adjusted accordingly.

## Acceptance criteria

- [x] A test registers Logging, Auditing, Authorization, Validation, and Transaction behaviors together via `InterlocutorBuilder.UseRecommendedChain()` (the real default registration path)
- [x] Sending a request through the composed pipeline produces an observed execution order matching Logging → Auditing → Authorization → Validation → Transaction (inbound), and the reverse on the way out
- [x] The test fails if any two adjacent stages are swapped, proving it actually asserts order and not just presence
- [x] Existing pipeline-ordering tests elsewhere in `tests/Interlocutor.DependencyInjection.Abstractions.Tests/` still pass

## Blocked by

None - can start immediately
