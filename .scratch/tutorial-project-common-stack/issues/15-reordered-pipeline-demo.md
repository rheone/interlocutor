# 15 - Reordered pipeline demo (Validation before Authorization)

**What to build:** A demonstration (a dedicated test or an isolated example composition, not necessarily a change to the live `Program.cs` pipeline, since that would affect every other feature's behavior) of composing Logging → Auditing → Validation → Authorization → Transaction instead of the recommended order, proving the mechanical consequence: a validation failure now short-circuits before authorization's ingress ever runs.

**Blocked by:** 05

**Status:** done

- [x] A test proves the reordered chain's short-circuit behavior differs from the recommended order's
- [x] A code comment states the trade-off explicitly: which behavior's cost is saved on a rejected request, and which is now paid on every request

## Comments

**2026-09-04** - Implemented as an isolated test, `tests/TodoTasks.Api.Tests/Unit/Infrastructure/ReorderedPipelineCompositionTests.cs` - `Program.cs`'s live pipeline is untouched. Composes the real `AuthorizationBehavior<,>`/`ValidationBehavior<,>` directly (no DI host) against a request that fails both checks: recommended order (Authorization outermost) denies without the validator ever running; reordered chain (Validation outermost) rejects without the policy evaluator ever being invoked (`DidNotReceive()`). An inline comment states the trade-off: the recommended order saves the validator's cost on a caller who was never authorized, at the cost of always paying authorization's (typically more expensive) check even on malformed input; the reordered chain inverts that. Full solution build (0 errors) and test (1189/1189) green.
