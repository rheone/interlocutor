Status: done

## Labels

ready-for-agent

## What to build

`documentation/beginners-guide-todo-api.md` Step 11's `SlowHandlerWarningBehavior<TRequest,TResponse>` example - a custom `IPipelineBehavior<TRequest,TResponse>` registered innermost (after `UseRecommendedChain()`'s five behaviors) that times only the handler itself and logs a warning past a threshold. Advanced-feature backlog item - demonstrates that pipeline position determines what a custom behavior actually measures, and how to add one alongside the recommended chain rather than replacing it.

## Acceptance criteria

- [x] `SlowHandlerWarningBehavior<TRequest,TResponse>` exists, registered innermost, with a test proving it measures only handler time (not the cost of the behaviors ahead of it)
- [x] A code comment explains why registration position - not a configuration flag - is what decides what the behavior measures, per the guide's own explanation

## Blocked by

12 (done - this ticket can start immediately)

## Comments

Promoted to `ready-for-agent` via `/to-tickets`, alongside 14-16, 18 - independent of each other, each already a single self-contained vertical slice.

Implemented: `Infrastructure/SlowHandlerWarningBehavior.cs`, exactly as written in the guide. Deliberately not wired into `Program.cs` - the guide's own note says this demonstrates composition, not something the finished project needs, and it isn't registered with any container in this project. Test coverage: `Unit/Infrastructure/SlowHandlerWarningBehaviorTests` - warns above the 250ms threshold, doesn't below it, and (the key case) doesn't warn when 300ms elapses *before* `Handle` is even called (simulating everything registered ahead of it in the chain) as long as the handler it actually wraps stays fast - proving the stopwatch starts at this behavior's own ingress, not the dispatch's start. `dotnet build`, `dotnet test` (967/967 solution-wide), `dotnet csharpier .` all clean.
