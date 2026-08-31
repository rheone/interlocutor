# 01 - Order-independent equality for the aggregate result types

**What to build:** `Failures`, `Failures<TErrorCode>`, `Conflicts`, `ValidationErrors`, `ValidationErrors<TErrorCode>`,
and `Interlocutor.FluentValidation.FluentValidationErrors` all compare equal when they hold the same distinct
items regardless of the order those items were collected in - e.g. two instances assembled from concurrent/async
work that completed in a different order are still the same result. Hash codes agree for equal instances. The
fix lives once in the shared internal `DistinctItemSet<TItem>` type these six types compose rather than in each
wrapper: overriding its compiler-synthesized record equality, which today compares its backing
`ImmutableArray<TItem>` by reference to the array instance, not by content - meaning two instances built from
identical items in identical order are not currently equal either, order aside.

Do not remove or reorder `Items`/enumeration - first-occurrence order is still preserved for display and
iteration; only *equality* becomes order-independent. If a caller needs a specific order for their own
comparison, they impose it themselves over `Items`.

**Blocked by:** None - can start immediately.

**Status:** ready-for-agent

- [ ] `DistinctItemSet<TItem>` has a content-based, order-independent `Equals`/`GetHashCode` (not the
      compiler-synthesized reference-based-on-backing-array version).
- [ ] Two instances of each of the six composing types (`Failures`, `Failures<TErrorCode>`, `Conflicts`,
      `ValidationErrors`, `ValidationErrors<TErrorCode>`, `FluentValidationErrors`) built from the same distinct
      items in different orders are equal and produce equal hash codes, with no changes made to any of the six
      wrapper types themselves.
  - [ ] Test coverage proves this (order-independent equality + hash-code agreement) for at least `Failures`
      and one of the generic (`<TErrorCode>`) variants.
- [ ] Test coverage proves the inverse: instances with extra items, missing items, or differing items are
      unequal.
- [ ] Default (zero-initialized) instances remain equal to each other and unequal to any explicitly-constructed
      instance, consistent with the existing "default value is inert" contract on `Count`/`Items`.
- [ ] `dotnet build` and `dotnet test` pass for the whole solution.
