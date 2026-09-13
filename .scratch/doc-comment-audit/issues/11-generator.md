# 11 — Audit and correct Generator's XML docs and code comments

**What to build:** Every public XML doc comment and code comment in `Interlocutor.Generator` accurately describes what the generator actually emits, with no claim that a union's first declared case type is inherently "the success case."

**Blocked by:** outcome-arity-and-factories/01 — Extend CommonKernel's shipped Outcome family to arity 16; outcome-arity-and-factories/02 — Add static factory methods for constructing Outcome instances of any shipped arity; aspnetcore-outcome-case-mapping-redesign/01 — Redesign Outcome-to-HTTP mapping to resolve by case type, never by position; aspnetcore-outcome-case-mapping-redesign/02 — Raise AspNetCore's shipped arity ceiling to match CommonKernel (this project's source is what those four tickets modify; its docs must describe the final emitted behavior, not a moving target).

**Status:** ready-for-agent

- [x] No doc comment describes a union's first declared case type as inherently "the success case" (including on internal model types describing case-type ordering, and on the result-extension emission logic) — case-type order is described as declaration order only, carrying no positional meaning.
- [x] Every doc comment and code comment in the project is reviewed against the full set of established XML-documentation and code-comment rules and corrected where it falls short.
- [x] Missing XML documentation on public members is added.
- [x] The project builds clean; existing tests are unaffected.
