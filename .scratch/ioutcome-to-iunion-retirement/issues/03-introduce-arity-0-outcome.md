# 03 — Introduce arity 0: a case-less `Outcome` acknowledgment

**What to build:** the non-generic `Outcome` type stops being a hardcoded `Success | Failure` union and becomes a genuinely case-less acknowledgment - no payload, no cases, no success/failure judgment of any kind. Holding an instance of the type is itself the signal that an operation ran; there is nothing to inspect on it. It implements `IUnion` directly (`Value` always `null`) purely to satisfy the response constraint, and is usable only by a handler that is genuinely infallible and opts into none of `ICancellable`/`IHasTimeout`/`IRateLimited` (none of those behaviors has a case to construct into a case-less type). Every existing internal usage of the old `Success | Failure` shape is migrated to whichever arity actually matches that call site's real failure modes.

**Blocked by:** 01, 02 (the new type only implements `IUnion`, not `IOutcome` - it fails to compile as a `TResponse` under the constraint these tickets replace).

**Status:** ready-for-agent

- [x] `Interlocutor.CommonKernel`'s non-generic `Outcome` is a small, hand-written type (not generator-emitted, since it declares no case types), implementing `System.Runtime.CompilerServices.IUnion` with `Value` always returning `null`.
- [x] `IOutcomeCaseRegistry.Construct<Outcome, TCase>` fails clearly (not silently) for any `TCase`, since there is no case to construct into - covered by a test.
- [x] A handler declaring `ICancellable`/`IHasTimeout`/`IRateLimited` and choosing the arity-0 `Outcome` as its result type produces a clear, build-time or documented-runtime signal that these are incompatible (matching how `IOutcomeCaseRegistry.Construct` already fails for a case a response type can't hold) - not a silent no-op when a short-circuit would otherwise fire.
- [x] Every doc example built around the retired `Success | Failure` `Outcome` for a fallible operation - the `ClearCache` worked example in `documentation/messages-and-result-types.md` and `documentation/beginners-guide-todo-api.md` - is updated to whichever arity actually matches: the case-less arity 0 if genuinely infallible, or an explicit `Outcome<Success, Failure>` (or richer arity) if it can fail.
- [x] `TutorialProject`'s existing consumers of the old `Outcome` shape are migrated the same way, and continue to compile and pass their tests.
- [x] `documentation/messages-and-result-types.md`'s "Result Types"/"Outcome Unions" sections and `documentation/composing-outcome-arities.md` describe arity 0 as the case-less acknowledgment, consistent with `CONTEXT.md`'s existing "Arity" glossary entry - extend that entry only if this work surfaces a term it doesn't yet cover, don't restate it differently.
- [x] Full unit test coverage for the new `Outcome`: satisfies `where TResponse : IUnion`; equality semantics (every instance is equal to every other, since there's no state to differ on); `ToString()` behavior.
- [x] `dotnet build` and `dotnet test` pass for the whole solution; `dotnet csharpier .` produces no diff.
