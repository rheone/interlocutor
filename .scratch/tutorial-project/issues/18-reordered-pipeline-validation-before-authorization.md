Status: ready-for-agent

## Labels

ready-for-agent

## What to build

`documentation/beginners-guide-todo-api.md` Step 11's "When the recommended order doesn't fit" section - a demonstration (likely a dedicated test or a documented alternate composition, not necessarily a change to the project's actual live `Program.cs` pipeline, since that would affect every other ticket's behavior) of composing Logging → Auditing → **Validation → Authorization** → Transaction instead of the recommended order, and proving the mechanical consequence: a validation failure now short-circuits *before* authorization's ingress ever runs. Advanced-feature backlog item - demonstrates that `UseRecommendedChain()` is opinionated sugar, not a constraint, and the genuine trade-off in swapping two of its behaviors.

## Acceptance criteria

- [ ] A test (or an isolated example composition, not necessarily wired into the live app) proves the reordered chain's short-circuit behavior differs from the recommended order's
- [ ] A code comment states the trade-off explicitly: which behavior's cost you're saving on a rejected request, and which you're now paying for on every one, matching the guide's own reasoning

## Blocked by

12 (done - this ticket can start immediately)

## Comments

Promoted to `ready-for-agent` via `/to-tickets`, alongside 14-17 - independent of each other, each already a single self-contained vertical slice.
