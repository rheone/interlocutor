# 08 - Extract RegistrationValidator out of InterlocutorBuilder

**What to build:** `InterlocutorBuilder.ValidateOnBuild()` keeps its current public behavior (aggregates every knowable registration-list problem into one exception) but the ~75 LOC of actual checking logic - `ComputeProblems` and its three independent checks (missing handler for a scanned request type, a required `AddTypeMatch` rule that matched nothing, two registrations of the same service/implementation pair disagreeing on lifetime) - moves out of `InterlocutorBuilder.cs` into a dedicated validator module that operates purely over the accumulated registration list. `ValidateOnBuild()` becomes a one-line forward to it, mirroring how `InterlocutorBuilder.Pipeline` already forwards pipeline composition to `InterlocutorPipelineBuilder` per ADR-0015.

**Blocked by:** None - can start immediately

- [ ] `ComputeProblems` and its supporting logic live in a new type (not a static method on `InterlocutorBuilder`), taking the registration list / candidate types it needs as input
- [ ] `InterlocutorBuilder.ValidateOnBuild()` forwards to the new type; its public signature and exception type/contents are unchanged
- [ ] `InterlocutorBuilder.cs` no longer contains the three-check validation logic
- [ ] The validation behavioral test matrix (all three problem kinds, aggregation into one exception) moves to a new dedicated test file, the same way `InterlocutorPipelineBuilderTests.cs` was split off `InterlocutorBuilderTests.cs` under ADR-0015; `InterlocutorBuilderTests.cs` retains only a smoke test that `ValidateOnBuild()` reaches the validator
- [ ] All existing DI-builder-and-discovery tests still pass, no public API changes beyond internal reorganization
