# 01 — Audit and correct CommonKernel's XML docs and code comments

**What to build:** Every public XML doc comment and code comment in `Interlocutor.CommonKernel` accurately describes the shipped API's actual contract and behavior, with no claim — explicit or hedged — that any generic-parameter position or arity implies success or failure.

**Blocked by:** outcome-arity-and-factories/01 — Extend CommonKernel's shipped Outcome family to arity 16; outcome-arity-and-factories/02 — Add static factory methods for constructing Outcome instances of any shipped arity (the API surface being documented must be settled first).

**Status:** done - implemented in `e76b595`

- [x] Every `<typeparam>` and `<remarks>` entry on `Outcome<T1,...,Tn>` describes each type parameter as an arbitrary, developer-chosen case value — no "by convention this position is success/failure" language, hedged or otherwise.
- [x] `Outcome<T>`'s documentation no longer claims the type "always succeeds" — it describes its actual behavior: a union with exactly one case, whose meaning is up to the caller.
- [x] No doc comment or code comment describes any member's implementation provenance (e.g. "hand-written" vs. generated) where that provenance isn't itself value-added information for the reader.
- [x] Every doc comment and code comment in the project is reviewed against the full set of established XML-documentation and code-comment rules — API contract over implementation, no restated signatures, documented preconditions/postconditions/exceptions/nullability/collection semantics where non-obvious, no narration, no unnecessary verbosity — and corrected where it falls short.
- [x] Missing XML documentation on public members is added.
- [x] The project builds with no new documentation-related warnings; existing tests are unaffected by these doc-only changes.
