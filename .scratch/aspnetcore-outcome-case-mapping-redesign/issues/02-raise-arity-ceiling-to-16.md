# 02 — Raise AspNetCore's shipped arity ceiling to match CommonKernel

**What to build:** `ToActionResult()`/`ToMinimalApiResult()` work for every `Outcome<T1,...,Tn>` arity CommonKernel ships (up to 16), using the corrected type-identity mapping rather than reintroducing position-based behavior at a wider ceiling.

**Blocked by:** outcome-arity-and-factories/01 — Extend CommonKernel's shipped Outcome family to arity 16 (CommonKernel must actually ship the wider arity range); 01 — Redesign Outcome-to-HTTP mapping to resolve by case type, never by position (the mapping must be corrected before it's extended).

**Status:** done - implemented in `0a927ab`

- [x] The generated `ToActionResult`/`ToMinimalApiResult` extensions exist and function correctly for every arity from 2 through 16, not just 2 through 6.
- [x] Each newly-generated high-arity extension resolves status codes via the shared type-identity mapping from ticket 01 — no separate or duplicated logic introduced for the new arities.
- [x] Tests cover at least one representative high arity (e.g. 16) exercising both a recognized-failure case and an unrecognized case, confirming the latter fails loudly rather than silently mapping to a status.
