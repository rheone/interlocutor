> **Status: open**

## Labels

ready-for-agent

## What to build

`DelegateRateLimiter`, `DelegateTimeoutPolicy`, and `DelegatePipelineStage<TRequest, TResponse>` are lambda-based shortcuts for `IRateLimiter`, `ITimeoutPolicy`, and a pipeline stage, respectively - letting a consumer supply a `Func<...>` instead of writing a full class for a simple case. `pipeline-and-behaviors.md` only ever shows the full-class form for all three; none of the three delegate types are mentioned.

Add a brief callout in each of the three relevant `pipeline-and-behaviors.md` subsections (`TimeoutBehavior`, `RateLimitedBehavior`, `Pipeline Stages`) showing the delegate-based shortcut alongside the existing full-class example - not replacing it, since the full form is still the right choice for anything with dependencies to inject.

## Acceptance criteria

- [ ] `DelegateRateLimiter`, `DelegateTimeoutPolicy`, `DelegatePipelineStage<,>` are each shown with a short example
- [ ] Existing full-class examples are kept, not replaced
