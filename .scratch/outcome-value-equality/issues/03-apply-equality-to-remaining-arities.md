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

**Status:** ready-for-agent

- [ ] `Outcome<TSuccessResult, TCase2, TCase3>` through `Outcome<TSuccessResult, TCase2, TCase3, TCase4, TCase5, TCase6>`
      all carry the generated equality members with no hand-written exceptions.
- [ ] At least one test demonstrates the end-to-end composition: an `Outcome<TSuccess, Failures>` (or another
      arity paired with `Conflicts`/`ValidationErrors`) is equal to another instance built from the same distinct
      items in a different order, and unequal to one built from different items.
- [ ] `PublicAPI.Unshipped.txt` is updated for the new members on every arity from three through six.
- [ ] `dotnet build` and `dotnet test` pass for the whole solution.
