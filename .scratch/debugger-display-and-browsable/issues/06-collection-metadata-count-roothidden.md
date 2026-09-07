# 06 - Collection-with-metadata types get the `Count`/`RootHidden` debugger pairing

**What to build:** `Page<T>`, the internal `DistinctItemSet<TItem>`, and the six plural aggregate result types
built on it (`Failures`, `Failures<TErrorCode>`, `Conflicts`, `ValidationErrors`, `ValidationErrors<TErrorCode>`,
`FluentValidationErrors`) debug the way BCL collection types do: a concise, count-based summary in the debugger's
Value column, and expanding the instance goes straight to its items instead of through a redundant intermediate
node - a private composed `DistinctItemSet<TItem>` field whose own `Count`/`Items` just duplicate what the outer
type already exposes.

**Blocked by:** Ticket 01.

**Status:** done

- [x] `Page<T>` carries a `[DebuggerDisplay]` summarizing page number, total pages, and item counts, and
      `[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]` on `Items` so expanding a `Page<T>` goes straight
      to its items.
- [x] `DistinctItemSet<TItem>` carries a `[DebuggerDisplay]` showing its `Count`, and
      `[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]` on `Items`.
- [x] Each of the six plural aggregate types has `[DebuggerBrowsable(DebuggerBrowsableState.Never)]` on its
      private `DistinctItemSet<TItem>`-typed backing field(s) - including the derived unique-error-code /
      unique-message caches on the `<TErrorCode>` generic variants - so expanding one of these types in the
      debugger shows only its public `Count`/`Items`/`Reasons`/`ErrorCodes` surface, not the internal composed
      field a second time.
- [x] Every `[DebuggerDisplay]` added in this ticket is verified via Ticket 01's helper.
- [x] No change to any type's public API, `ToString()`, or the order-independent equality these types already
      have (see the prior `outcome-value-equality` effort) - this ticket is debug-view-only.
- [x] `dotnet build` and `dotnet test` pass for the whole solution.

## Comments

**2026-09-04** - Re-triaged while working the frontier: already implemented in commit `9b27d74`. `Page<T>` carries a page/count `[DebuggerDisplay]` and `[DebuggerBrowsable(RootHidden)]` on `Items`; `DistinctItemSet<TItem>` carries `[DebuggerDisplay("Count = {Count}")]` and `RootHidden` on `Items`; all six plural aggregate types (`Failures`, `Failures<TErrorCode>`, `Conflicts`, `ValidationErrors`, `ValidationErrors<TErrorCode>`, `FluentValidationErrors`) have `[DebuggerBrowsable(Never)]` on their composed backing fields, including the derived unique-code/message caches. Verified in `tests/Interlocutor.CommonKernel.Tests/CollectionMetadataDebuggerDisplayTests.cs` and `tests/Interlocutor.Persistence.Abstractions.Tests/PageDebuggerDisplayTests.cs`. No public API/ToString/equality change. No source changes were needed this session.
