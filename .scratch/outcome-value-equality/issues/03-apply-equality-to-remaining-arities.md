# 03 - Apply generated equality to the remaining shipped arities (3-6 cases)

**What to build:** Every shipped `Outcome<...>` arity - three through six case types - carries the identical
generated `Equals`/`GetHashCode`/`IEquatable`/`==`/`!=` surface proven in Ticket 02 on the two-case arities, with
no hand-written exceptions anywhere in the family.

This ticket also delivers the integration proof this whole effort was motivated by: composing Ticket 01's
order-independent aggregate equality with Ticket 02's union-level equality. An `Outcome<TSuccess, Failures>` (or
`Conflicts`/`ValidationErrors`) built the same way except for the order its aggregate case's items were
collected in must compare equal - proving the union's delegation to `Value.Equals(other.Value)` cascades
correctly through a collection-bearing nested case, not just a flat one.

**Blocked by:** Ticket 02.

**Status:** done (already implemented)

- [x] `Outcome<TSuccessResult, TCase2, TCase3>` through `Outcome<TSuccessResult, TCase2, TCase3, TCase4, TCase5, TCase6>`
      all carry the generated equality members with no hand-written exceptions.
- [x] At least one test demonstrates the end-to-end composition: an `Outcome<TSuccess, Failures>` (or another
      arity paired with `Conflicts`/`ValidationErrors`) is equal to another instance built from the same distinct
      items in a different order, and unequal to one built from different items.
- [x] `PublicAPI.Unshipped.txt` is updated for the new members on every arity from three through six.
- [x] `dotnet build` and `dotnet test` pass for the whole solution.

## Comments

**2026-09-04** - Re-triaged while working the frontier: already implemented. Every arity three through six in `src/Interlocutor.CommonKernel/Outcome.cs` is an attributed `partial union` (see Ticket 06 of `outcome-naming-and-arity-refactor`), so the generated equality surface applies uniformly with no hand-written exceptions. `OutcomeEqualityTests.cs` includes the integration proof: `Outcome<string, Failures>` built from `[failureA, failureB]` vs `[failureB, failureA]` compares equal, and a differing-items instance compares unequal - proving the union's `Value.Equals(other.Value)` delegation cascades through `Failures`' order-independent equality. `PublicAPI.Unshipped.txt` carries `Equals`/`GetHashCode` entries for arities three through six. No source changes were needed - Status flipped without a code commit.
