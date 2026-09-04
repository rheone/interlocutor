# 14 - Custom pipeline behavior demo (slow-handler warning)

**What to build:** A second custom `IPipelineBehavior` beyond the recommended chain (Logging → Auditing → Authorization → Validation → Transaction), demonstrating how to add one of your own - a slow-handler warning behavior that logs when a handler exceeds a threshold duration, per the original guide's own example.

**Blocked by:** 05

**Status:** done

- [x] The behavior is registered and demonstrably runs in the pipeline (a test proving it fires above the threshold and not below it)
- [x] A code comment explains where it sits in the onion ordering and why

## Comments

**2026-09-04** - Implemented: `SlowHandlerWarningBehavior<TRequest, TResponse>` times the inner `next()` call and logs a warning above a 250ms threshold. Registered innermost (last, after `TodoTasksUnitOfWorkBehavior<,>`), matching the guide's rationale that its position decides what it measures - last means it isolates the handler's own cost from every other behavior's overhead. Three tests prove: no warning below threshold, a warning above threshold, and - the ordering-isolation proof - a slow *ingress* elsewhere (simulated ahead of this behavior) paired with a fast handler still doesn't fire, since the stopwatch only starts at this behavior's own position. No real near-threshold sleeps, so no flake risk. Full solution build (0 errors) and test (1187/1187) green.
