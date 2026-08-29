> **Status: done**

## Labels

ready-for-agent

## What to build

`DelegateRateLimiter`, `DelegateTimeoutPolicy`, and `DelegatePipelineStage<TRequest, TResponse>` are lambda-based shortcuts for `IRateLimiter`, `ITimeoutPolicy`, and a pipeline stage, respectively - letting a consumer supply a `Func<...>` instead of writing a full class for a simple case. `pipeline-and-behaviors.md` only ever shows the full-class form for all three; none of the three delegate types are mentioned.

Add a brief callout in each of the three relevant `pipeline-and-behaviors.md` subsections (`TimeoutBehavior`, `RateLimitedBehavior`, `Pipeline Stages`) showing the delegate-based shortcut alongside the existing full-class example - not replacing it, since the full form is still the right choice for anything with dependencies to inject.

## Acceptance criteria

- [x] `DelegateRateLimiter`, `DelegateTimeoutPolicy`, `DelegatePipelineStage<,>` are each shown with a short example
- [x] Existing full-class examples are kept, not replaced

## Notes

`DelegatePipelineStage<,>`'s own XML doc says it's "the type backing lambda-form stage registration (for example, `InterlocutorPipelineBuilder.AddStage`'s delegate overloads)" - so the idiomatic form is calling `AddStage(onBefore, onAfter)` directly with lambdas, never constructing `DelegatePipelineStage` by name. Documented that way, plus a pointer to the second `IServiceProvider`-receiving overload for stages needing scoped dependencies.

`DelegateRateLimiter`/`DelegateTimeoutPolicy` have no equivalent delegate-friendly builder overload - `OverrideRateLimiter<T>()`/`OverrideTimeoutPolicy<T>()` are type-parameter-based (DI-constructed), which can't work for a type whose constructor takes a `Func`. Documented the correct pattern instead: register the instance directly with the container before `AddInterlocutor()` runs, so the behavior's own try-added default is skipped by existing try-add semantics.
