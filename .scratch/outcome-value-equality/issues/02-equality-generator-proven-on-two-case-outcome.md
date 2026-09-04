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

**Status:** done (already implemented)

- [x] `OutcomeMemberEmitter` emits `Equals(object?)`, `Equals(TSelf other)`, `GetHashCode()`, `IEquatable<TSelf>`,
      `operator ==`, and `operator !=` for an attributed union, delegating to the boxed `Value`.
- [x] Applied to the non-generic `Outcome` and to `Outcome<TSuccessResult, TFailureResult>` only in this ticket.
- [x] `OutcomeMemberGeneratorTests` covers the new emitted members (generator-output tests).
- [x] `Interlocutor.CommonKernel.Tests` covers real behavior: two instances holding equal case values are equal
      with equal hash codes; two instances holding different case values are unequal; a default
      (zero-initialized) instance behaves consistently with its documented inert default; boxing a value into
      one arity is never equal to the "same" value in a different-arity union.
- [x] `src/Interlocutor.CommonKernel/PublicAPI.Unshipped.txt` (or wherever the analyzer expects it) is updated
      for the new public members on both arities.
- [x] `dotnet build` and `dotnet test` pass for the whole solution.

## Comments

**2026-09-04** - Re-triaged while working the frontier: already implemented, and folded straight into the full-family form (Ticket 03's scope) rather than landing on just the two arities first. `OutcomeMemberEmitter` (`src/Interlocutor.Generator/Emission/`) emits `Equals`/`GetHashCode`/`IEquatable<TSelf>`/`==`/`!=` delegating to the boxed `Value`; `OutcomeMemberGeneratorTests` covers the generator output and `OutcomeEqualityTests.cs` covers real behavior including cross-case-type and cross-arity non-equality. `PublicAPI.Unshipped.txt` carries the new members for `Outcome` and `Outcome<TSuccessResult, TFailureResult>`. No source changes were needed - Status flipped without a code commit.
