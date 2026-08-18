## Labels

ready-for-agent

## What to build

Remove three pieces of dead code identified by audit:

1. The commented-out `ToString()` override in `src/Interlocutor.CommonKernel/ResultTypes/Failure.cs` (lines 66-80) - dead since before the "Interlocutor" rename, no sibling result type (`NotFound`, `Forbidden`, `Conflict`, `Success`, `ValidationError`) has an equivalent override, and it isn't referenced anywhere.
2. `NoOpAuditSink` (`src/Interlocutor.Abstractions/Pipeline/NoOpAuditSink.cs`) - zero references outside its own file, no doc example, no test; documentation shows consumers hand-rolling `IAuditSink` implementations instead.
3. `DelegatePolicyEvaluator` (`src/Interlocutor.Abstractions/Pipeline/DelegatePolicyEvaluator.cs`) - same pattern; documentation shows consumers hand-rolling `IPolicyEvaluator` implementations instead.
4. `src/Interlocutor.DependencyInjection.Abstractions/DefaultPipelineRegistrations.cs` (added 2026-08-17) - a parallel, unused catalog of the same default behaviors/dependencies `InterlocutorBuilder` already registers directly. Referenced only from `<see cref>` XML doc comments inside `InterlocutorBuilder.cs`, never called by either `AddInterlocutor` entry point (Microsoft or Autofac) - confirmed by reading both `ServiceCollectionExtensions.cs` and `ContainerBuilderExtensions.cs` in full. Its own dedicated test file (`DefaultPipelineRegistrationsTests.cs`) only exercises the orphaned class in isolation, never through a real registration path.

## Acceptance criteria

- [x] The commented-out `ToString()` block is deleted from `Failure.cs` (already absent from the current file - nothing to delete; confirmed no sibling result type has an equivalent override)
- [x] `NoOpAuditSink.cs` is deleted, and any reference to it (registration defaults, DI wiring) is confirmed absent first
- [x] `DelegatePolicyEvaluator.cs` is deleted, and any reference to it is confirmed absent first
- [x] `DefaultPipelineRegistrations.cs` is deleted along with its dedicated test file (`DefaultPipelineRegistrationsTests.cs`) - confirmed by reading `ServiceCollectionExtensions.cs` and `ContainerBuilderExtensions.cs` in full that neither `AddInterlocutor` entry point calls it; only the three `<see cref>` XML doc references inside `InterlocutorBuilder.cs` pointed to it, and those were rewritten to drop the dangling cref
- [x] Full solution build and test run still pass after removal
- [x] No documentation (`README.md`, `documentation/`) references any deleted type

## Blocked by

None - can start immediately
