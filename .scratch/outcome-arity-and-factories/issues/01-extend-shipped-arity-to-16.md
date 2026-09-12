# 01 — Extend CommonKernel's shipped Outcome family to arity 16

**What to build:** CommonKernel ships a working `Outcome<T1,...,Tn>` for every arity from 2 through 16, not just 6, each fully functional via the existing generator-driven `Match`/`Switch`/equality members — a consumer can declare and use an `Outcome` of any arity up to the actual ceiling the `union` language feature and the BCL's delegate types support, not an artificially narrower one.

**Blocked by:** None — can start immediately.

**Status:** done - implemented in `239b522`

- [x] Every one of `Outcome<T1,T2>` through `Outcome<T1,...,T6>` carries both the `[GenerateOutcomeMembers]` attribute and the `[Union]` attribute consistently (finishing the in-progress work already present on disk).
- [x] New `Outcome<T1,...,Tn>` declarations exist for every arity from 7 through 16, each carrying the same `[GenerateOutcomeMembers][Union]` pair and producing a working generated `Match`/`Switch`/equality surface via the existing generator — no generator changes are required, since it already supports arbitrary arity up to the BCL's 16-parameter ceiling.
- [x] CommonKernel's own project-level configuration raises the generator's recommended-arity warning threshold so CommonKernel's own 7–16-arity declarations do not produce a build warning on CommonKernel itself, while the solution-wide default threshold for consumers declaring their own unions remains unchanged at 6.
- [x] The solution builds clean and all existing CommonKernel tests pass; new tests cover construction, `Match`, `Switch`, and equality for at least one representative high arity (e.g. 16) in addition to the existing low-arity coverage.
