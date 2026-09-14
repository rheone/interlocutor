# 03 — Audit and correct AspNetCore's XML docs and code comments

**What to build:** Every public XML doc comment and code comment in `Interlocutor.AspNetCore` accurately describes the corrected, type-identity-based HTTP mapping behavior, and makes clear that HTTP mapping is an optional, package-specific convenience rather than a property of `Outcome`/`union` in general.

**Blocked by:** aspnetcore-outcome-case-mapping-redesign/01 — Redesign Outcome-to-HTTP mapping to resolve by case type, never by position; aspnetcore-outcome-case-mapping-redesign/02 — Raise AspNetCore's shipped arity ceiling to match CommonKernel (the behavior being documented must be settled first).

**Status:** done - implemented in `be32d1a`

- [x] Documentation describes the HTTP mapping mechanism accurately: type-identity-based, covering a known closed set of case types, throwing for anything outside it — no claim that any position or arity slot is inherently the success/200 case.
- [x] Documentation makes clear that HTTP-status mapping is a convenience specific to API/web contexts, not a guaranteed or default feature of every `Outcome`.
- [x] Every doc comment and code comment in the project is reviewed against the full set of established XML-documentation and code-comment rules and corrected where it falls short.
- [x] Missing XML documentation on public members is added.
- [x] The project builds clean; existing tests are unaffected.
