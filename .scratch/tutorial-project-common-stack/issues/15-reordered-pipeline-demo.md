# 15 - Reordered pipeline demo (Validation before Authorization)

**What to build:** A demonstration (a dedicated test or an isolated example composition, not necessarily a change to the live `Program.cs` pipeline, since that would affect every other feature's behavior) of composing Logging → Auditing → Validation → Authorization → Transaction instead of the recommended order, proving the mechanical consequence: a validation failure now short-circuits before authorization's ingress ever runs.

**Blocked by:** 05

**Status:** ready-for-agent

- [ ] A test proves the reordered chain's short-circuit behavior differs from the recommended order's
- [ ] A code comment states the trade-off explicitly: which behavior's cost is saved on a rejected request, and which is now paid on every request
