## Labels

ready-for-agent

## What to build

Add an end-to-end test that wires the real Logging, Auditing, Authorization, Validation, and Transaction behaviors together in the documented canonical order and asserts the observed execution order via an actual `Send`, not just a list of registered behavior types.

`tests/Interlocutor.DependencyInjection.Abstractions.Tests/DefaultPipelineRegistrationsTests.cs::Behaviors_PreservesCanonicalOrder_LoggingThenAuditingThenAuthorization` only checks the first 3 of 5 stages, and does so at the registration-list level - it never runs a request through the composed pipeline. No test anywhere registers all five real behaviors together and records the order they actually execute in.

## Acceptance criteria

- [ ] A test registers Logging, Auditing, Authorization, Validation, and Transaction behaviors together via the builder's default registration path
- [ ] Sending a request through the composed pipeline produces an observed execution order matching Logging → Auditing → Authorization → Validation → Transaction (inbound), and the reverse on the way out
- [ ] The test fails if any two adjacent stages are swapped, proving it actually asserts order and not just presence
- [ ] Existing `DefaultPipelineRegistrationsTests.cs` assertions still pass

## Blocked by

None - can start immediately
