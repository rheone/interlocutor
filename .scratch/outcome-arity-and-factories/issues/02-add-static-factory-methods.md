# 02 — Add static factory methods for constructing Outcome instances of any shipped arity

**What to build:** A consumer constructing an `Outcome` of a given arity can do so via a discoverable static factory method rather than only the positional constructor/implicit-conversion path, for every shipped arity from 0 through 16.

**Blocked by:** 01 — Extend CommonKernel's shipped Outcome family to arity 16 (the full 0–16 arity range must exist first).

**Status:** done - implemented in `5ebe2c7`

- [x] Static factory methods exist for `Outcome` (arity 0) and `Outcome<T>` (arity 1), hand-written alongside their existing hand-written members.
- [x] Static factory methods for arities 2 through 16 are emitted by the source generator alongside the existing `Match`/`Switch`/equality members, rather than hand-maintained per arity.
- [x] None of the added factory methods collide in name or signature with any member the `union` language feature or the existing generator output already provides (e.g. implicit conversion operators, existing constructors) — adding a factory must not create ambiguity at a call site that previously compiled.
- [x] Tests demonstrate constructing a value via the factory for at least a low arity and a high arity, and confirm the resulting instance behaves identically to one constructed the existing way.
