Status: backlog - deferred, do not start until explicitly promoted to ready-for-agent

## Labels

needs-triage

## What to build

`documentation/beginners-guide-todo-api.md` Step 11's `SlowHandlerWarningBehavior<TRequest,TResponse>` example - a custom `IPipelineBehavior<TRequest,TResponse>` registered innermost (after `UseRecommendedChain()`'s five behaviors) that times only the handler itself and logs a warning past a threshold. Advanced-feature backlog item - demonstrates that pipeline position determines what a custom behavior actually measures, and how to add one alongside the recommended chain rather than replacing it.

## Acceptance criteria

- [ ] `SlowHandlerWarningBehavior<TRequest,TResponse>` exists, registered innermost, with a test proving it measures only handler time (not the cost of the behaviors ahead of it)
- [ ] A code comment explains why registration position - not a configuration flag - is what decides what the behavior measures, per the guide's own explanation

## Blocked by

12
