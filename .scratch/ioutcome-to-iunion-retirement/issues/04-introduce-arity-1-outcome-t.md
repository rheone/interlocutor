# 04 — Introduce arity 1: `Outcome<T>`

**What to build:** a single, always-succeeding payload wrapper for an operation that is genuinely infallible but still produces exactly one value - so it no longer has to declare an unreachable failure case just to satisfy the response constraint. Hand-written, not routed through `OutcomeMemberGenerator`/`[GenerateOutcomeMembers]` (the generator's existing `INTERLOC011` floor of two case types is correct for a consumer's attributed union and is left completely unchanged - it simply doesn't apply to this deliberately hand-written shipped type). Exposes the same member shape as every other arity so a caller never has to special-case it.

**Blocked by:** 01, 02 (needs to be usable as a real handler response to be demoed end-to-end, and the old constraint would reject an `IUnion`-only type).

**Status:** ready-for-agent

- [ ] `Interlocutor.CommonKernel` gains `Outcome<T>`: a strongly-typed public `T Value` property (not boxed); an explicit `object? System.Runtime.CompilerServices.IUnion.Value` implementation forwarding to it; `TResult Match<TResult>(Func<T, TResult> onValue)`; `void Switch(Action<T> onValue)`; `bool TryGetValue(out T value)` (always returns `true`); `ToString()` forwarding to `Value?.ToString()`.
- [ ] Value-equality shape matches every other arity: `Equals`, `GetHashCode`, `IEquatable<Outcome<T>>`, `==`/`!=`, all delegating to `Value`'s own equality.
- [ ] `INTERLOC011` (the generator's floor of two case types) is unmodified and still fires for a consumer's own attributed single-case union - confirmed by its existing test suite passing unchanged.
- [ ] A demonstration handler using `Outcome<T>` as its response type dispatches correctly through the full pipeline (a real end-to-end proof this is legal as `TResponse`, not just a `Interlocutor.CommonKernel`-local unit test).
- [ ] Full unit test parity with the existing arity-2-through-6 test suites for `Match`/`Switch`/`TryGetValue`/`ToString`/equality, adapted for the single-case shape (`TryGetValue` always succeeds; `Match`/`Switch` take exactly one delegate).
- [ ] `documentation/messages-and-result-types.md`'s "Result Types"/"Outcome Unions" sections and `documentation/composing-outcome-arities.md` document arity 1 alongside arity 0, consistent with `CONTEXT.md`'s existing "Arity" glossary entry.
- [ ] `PublicAPI.Unshipped.txt` (`Interlocutor.CommonKernel`) updated for the new type and its members.
- [ ] `dotnet build` and `dotnet test` pass for the whole solution; `dotnet csharpier .` produces no diff.
