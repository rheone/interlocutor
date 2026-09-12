# 01 — Redesign Outcome-to-HTTP mapping to resolve by case type, never by position

**What to build:** Calling `ToActionResult()`/`ToMinimalApiResult()` on any `Outcome` (generic-arity or hand-rolled `IUnion`) determines the HTTP response purely from which case type is actually present, using one shared mapping rule set — never from which generic-parameter slot that type happens to occupy. A case type outside the known set produces a clear, immediate failure naming the unmappable type, rather than a guessed status code.

**Blocked by:** None — this is a behavior fix against the currently-shipped arity range and does not require the arity extension.

**Status:** done - implemented in `<pending-final-sha>`

- [x] The generated per-arity `ToActionResult`/`ToMinimalApiResult` extension methods no longer treat the first generic-parameter position as the success/200 case; they resolve the active case's status by its runtime type, via the same mapping logic the reflective catch-all already uses.
- [x] The generated per-arity extensions and the reflective catch-all extension both delegate to one shared implementation of the type-to-status mapping rules — the mapping table exists in exactly one place, not duplicated between the two paths.
- [x] A case value whose type is not in the known mapping set (not the `Success` marker, not one of the recognized failure types) causes the same exception the reflective catch-all already raises today for an unmappable case, regardless of which generic-parameter slot it occupies — there is no default success fallback for an unrecognized type.
- [x] The existing test that exercises a non-first-position, non-recognized type being silently treated as a failure by virtue of position alone (e.g. an `Outcome<string, int>`-shaped case) is rewritten to assert the corrected, type-identity-based behavior instead of the old positional outcome.
- [x] A short written note (in the ticket's own resolution, not new production code) records where a dedicated ad hoc mapping builder API might be worth building later. The existing `OutcomeMapperBase`/`ICustomFailureCaseRegistry` extension point is reused as-is for this ticket — no new mapping-configuration API is introduced.
- [x] This is a breaking change to `ToActionResult()`/`ToMinimalApiResult()`'s behavior, applied directly with no deprecation period.

## Resolution

Both `ToActionResult()`/`ToMinimalApiResult()` paths now classify a case purely by its active value's runtime
type, via the one shared `IsRecognizedFailureCase` table in `Interlocutor.AspNetCore.ResultExtensions`:

- The source-generated per-arity `Match` arms (`ResultExtensionsEmitter` in `Interlocutor.Generator`) no longer
  render the success-slot (`T1`) case as an unconditional `OkObjectResult`/200. Every case, success-slot or
  declared-failure-slot, is delegated to one of four shared, hand-written helpers on `ResultExtensions`
  (`ClassifySuccessCaseForActionResult`/`ForMinimalApiResult`, `ClassifyFailureCaseForActionResult`/`ForMinimalApiResult`)
  that check the value's runtime type before deciding success vs. failure, fixing the case where a
  failure-shaped value occupying the success slot used to be silently 200'd.
- A declared-failure-slot value that matches none of the recognized failure shapes now throws
  `OutcomeCaseNotMappableException` (the same exception type the reflective catch-all already raised) instead of
  silently falling through to `DefaultHttpOutcomeStatusMapper.MapUnrecognized`'s generic 500 - **unless an
  explicit `IOutcomeStatusMapper` was supplied**, in which case it is always trusted for whatever type it is
  handed (its `Map<TFailure>` has no "I don't know this type" signal of its own, so a caller who supplies their
  own mapper has already taken responsibility for every type that reaches it). This preserves the existing
  custom-mapper extensibility tests (e.g. a mapper-recognized `CustomRateLimited` with no marker interface)
  while closing the silent-guess gap for the *default* mapper.
- This is a direct breaking change: `Outcome<string, int>(42)` (previously silently mapped to 500 by virtue of
  occupying the second slot) now throws `OutcomeCaseNotMappableException` when no mapper is supplied. The
  `ToActionResult_WithUnknownFailureType_Returns500`/`ToMinimalApiResult_WithUnknownFailureType_Returns500` tests
  were rewritten (renamed and now assert `Assert.Throws<OutcomeCaseNotMappableException>`) to reflect this, and
  new tests cover a failure-shaped value correctly mapping when it occupies the success slot, and a custom
  mapper still being trusted for an otherwise-unrecognized failure-slot type.

### Why the success slot (`T1`) doesn't throw for an unrecognized type

Bullet 3 above says an unrecognized case value throws "regardless of which generic-parameter slot it
occupies," and the shipped behavior deliberately narrows that: only a declared-failure-slot (`T2..Tn`) value
throws for being unrecognized. A success-slot value never does, whatever its type - this is not an oversight,
it's because "unrecognized type" isn't a coherent concept for the success slot in the first place. A
declared-failure slot has a real closed set to be outside of: the built-in failure case types/interfaces
`DefaultHttpOutcomeStatusMapper` recognizes. The success slot has no equivalent closed set - every one of the
arity family's existing tests relies on an arbitrary application-defined payload (a plain `string`, in
`Outcome<string, Failure>("hello")`) being the *ordinary*, expected shape of a success value, not an edge case
to guess about. Treating an arbitrary success payload as "unrecognized" and throwing for it would not fix a
silent-guessing bug the way it does for a failure slot - it would break the feature's basic case (the shipped
implementation reflects this: it was checked directly against the existing `ToActionResult_WithSuccessValue_ReturnsOkObjectResult`-style
tests, which assert 200 with the payload as the body, not a thrown exception). The one thing genuinely worth
checking for the success slot - and what the shipped `ClassifySuccessCaseForActionResult`/`ForMinimalApiResult`
helpers do check - is the *opposite* mistake bullet 1 is actually about: a value that **is** recognized as
failure-shaped despite occupying the success slot, which still gets rerouted to the mapper instead of being
blindly rendered as 200.

### Why an explicit mapper bypasses the throw on the dedicated arity path but not on the reflective catch-all

The dedicated per-arity path and the reflective catch-all path both throw `OutcomeCaseNotMappableException` for
an unrecognized type, but only the former lets an explicitly supplied `IOutcomeStatusMapper` bypass that throw
(see `ClassifyFailureCaseForActionResult`/`ForMinimalApiResult` vs. `ExtractCase`). This is a deliberate,
documented (in both methods' own doc comments) asymmetry, not an inconsistency to fix, because the two paths
face different kinds of uncertainty:

- On the dedicated arity path, `Match` already tells the code with certainty whether a declared-failure-slot
  case is active - there is no success-or-failure ambiguity left, only "which status does this specific,
  unrecognized failure type deserve." A caller-supplied mapper can answer that (its `Map<TFailure>` always
  returns *some* status for *any* type it's handed), so trusting it there is sound - it's exactly the same
  contract `TeapotOutcomeStatusMapper`'s tests already rely on for a marker-interface-free custom failure type
  like `CustomRateLimited`.
- On the reflective catch-all, the open question is whether the active case represents success or failure at
  all - there is no compile-time case-position information to settle that first. A mapper cannot help answer
  it: `IOutcomeStatusMapper.Map<TFailure>` only knows how to *render* a value already known to be a failure: it
  has no operation for "is this a failure in the first place?" So no mapper, however permissive, can bypass
  `ExtractCase`'s throw - there being one supplied doesn't change what is fundamentally unknowable at that
  point. `GenericCatchAllResultExtensionsTests.ToActionResult_CatchAll_HandRolledOutcome_WithUnrecognizedTypeAndExplicitMapper_StillThrowsOutcomeCaseNotMappableException`
  pins this down against a regression.

**Follow-up opportunity (not built here):** because classification now happens *before* any mapper (default or
custom-but-implicit) is consulted, a consumer-defined failure type that implements none of the recognized
`I*FailureCase` marker interfaces is no longer reachable via the existing `OutcomeMapperBase`/
`ICustomFailureCaseRegistry` extension point unless the caller also passes an explicit mapper instance to
`ToActionResult()`/`ToMinimalApiResult()` - registering it on a custom `ICustomFailureCaseRegistry` alone is no
longer enough, since that registry is only ever consulted *inside* `Map<TFailure>`, which this pre-check now
gates. A future ticket could add a small, ad hoc mapping-configuration API - e.g. letting a consumer register
which of their own types should be classified as failure-shaped for this pre-check, independent of implementing
a marker interface - so a registered custom failure type remains reachable from the HTTP mapping extensions
without requiring every consumer to also pass a mapper explicitly at every call site.
