> **Status: done**

## Labels

ready-for-agent

## What to build

New `documentation/extensibility.md` covering how a consumer registers custom types against the library's extension points:

- Custom marker interfaces as the opt-in mechanism for cross-cutting behavior (pattern, not just the shipped set).
- Registering a custom `IPipelineBehavior<TRequest, TResponse>` or lightweight `IPipelineStage<TRequest, TResponse>`.
- Registering a custom `IOperationOutcome` case via `IOutcomeCaseRegistry`/`OutcomeCaseRegistryBuilder`, and classifying it via `IOutcomeClassifier`/`OutcomeClassifierBuilder`.
- `InterlocutorBuilder.AddTypeMatch` for predicate-based registration beyond contract-interface matching.
- `InterlocutorPipelineBuilder.OverridePipelineFor` for per-request-type pipeline overrides.
- Where each hook is discovered/invoked in `Interlocutor.Abstractions` and `Interlocutor.DependencyInjection.Abstractions`, so a reader can trace the mechanism, not just copy an example.

Link it from `documentation/index.md`. Cross-link from `builder-and-registration.md` and `pipeline-and-behaviors.md` rather than duplicating content.

## Acceptance criteria

- [x] `documentation/extensibility.md` exists, linked from `documentation/index.md`
- [x] Every registration pattern shown compiles against the current shipped API
- [x] No duplication with existing DI/pipeline docs beyond a short cross-link

## Notes

Caught and fixed one bug while drafting: `OverrideOutcomeCaseRegistry<TRegistry>()` takes a type parameter (constructed through DI), not a pre-built instance - my first draft passed a `new OutcomeCaseRegistryBuilder()...Build()` result directly. Fixed by wrapping the built registry in a small class and passing that as the type argument, matching the real signature. Also added `RequireUniqueClosure`/`Eject` (from unit 03's findings) and cross-linked from `builder-and-registration.md`/`pipeline-and-behaviors.md`.
