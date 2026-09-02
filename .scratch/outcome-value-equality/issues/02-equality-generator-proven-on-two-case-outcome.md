# 02 - Prove generated equality on the two-case `Outcome` arities

**What to build:** `Outcome` and `Outcome<TSuccessResult, TFailureResult>` gain value-based equality -
`Equals(object?)`, a strongly-typed `Equals`, `GetHashCode`, `IEquatable<TSelf>`, and `==`/`!=` - source-generated
by `GenerateOutcomeMembersAttribute`/`OutcomeMemberGenerator` alongside the existing `Match`/`Switch`/
`TryGetValue`/`ToString`/`IsSuccess` members, not hand-written. Each member delegates to the boxed case value's
own equality (`this.Value`/`other.Value`), since every built-in case type is already a correctly-behaved
`readonly record struct`; no per-case-type switch is needed at this layer. The existing `OutcomeUnionModel`
already captures everything the emitter needs (the union type and its case list) - no prefactor required.

Two different case types boxed into the same union arity must never compare equal even by coincidence, and a
union built from one case type must never equal a different-arity union holding the "same" case value - both
follow automatically from checking exact union-type identity before delegating, the same way record equality
works.

**Blocked by:** None - can start immediately.

**Status:** ready-for-agent

- [ ] `OutcomeMemberEmitter` emits `Equals(object?)`, `Equals(TSelf other)`, `GetHashCode()`, `IEquatable<TSelf>`,
      `operator ==`, and `operator !=` for an attributed union, delegating to the boxed `Value`.
- [ ] Applied to the non-generic `Outcome` and to `Outcome<TSuccessResult, TFailureResult>` only in this ticket.
- [ ] `OutcomeMemberGeneratorTests` covers the new emitted members (generator-output tests).
- [ ] `Interlocutor.CommonKernel.Tests` covers real behavior: two instances holding equal case values are equal
      with equal hash codes; two instances holding different case values are unequal; a default
      (zero-initialized) instance behaves consistently with its documented inert default; boxing a value into
      one arity is never equal to the "same" value in a different-arity union.
- [ ] `src/Interlocutor.CommonKernel/PublicAPI.Unshipped.txt` (or wherever the analyzer expects it) is updated
      for the new public members on both arities.
- [ ] `dotnet build` and `dotnet test` pass for the whole solution.
