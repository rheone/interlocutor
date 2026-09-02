# 06 - Collection-with-metadata types get the `Count`/`RootHidden` debugger pairing

**What to build:** `Page<T>`, the internal `DistinctItemSet<TItem>`, and the six plural aggregate result types
built on it (`Failures`, `Failures<TErrorCode>`, `Conflicts`, `ValidationErrors`, `ValidationErrors<TErrorCode>`,
`FluentValidationErrors`) debug the way BCL collection types do: a concise, count-based summary in the debugger's
Value column, and expanding the instance goes straight to its items instead of through a redundant intermediate
node - a private composed `DistinctItemSet<TItem>` field whose own `Count`/`Items` just duplicate what the outer
type already exposes.

**Blocked by:** Ticket 01.

**Status:** ready-for-agent

- [ ] `Page<T>` carries a `[DebuggerDisplay]` summarizing page number, total pages, and item counts, and
      `[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]` on `Items` so expanding a `Page<T>` goes straight
      to its items.
- [ ] `DistinctItemSet<TItem>` carries a `[DebuggerDisplay]` showing its `Count`, and
      `[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]` on `Items`.
- [ ] Each of the six plural aggregate types has `[DebuggerBrowsable(DebuggerBrowsableState.Never)]` on its
      private `DistinctItemSet<TItem>`-typed backing field(s) - including the derived unique-error-code /
      unique-message caches on the `<TErrorCode>` generic variants - so expanding one of these types in the
      debugger shows only its public `Count`/`Items`/`Reasons`/`ErrorCodes` surface, not the internal composed
      field a second time.
- [ ] Every `[DebuggerDisplay]` added in this ticket is verified via Ticket 01's helper.
- [ ] No change to any type's public API, `ToString()`, or the order-independent equality these types already
      have (see the prior `outcome-value-equality` effort) - this ticket is debug-view-only.
- [ ] `dotnet build` and `dotnet test` pass for the whole solution.
