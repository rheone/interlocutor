# 05 - Arity-member generator, proven on `Outcome<TSuccess, TFailure>`

**What to build:** Today each arity of `Outcome<...>` hand-writes its own `IsSuccess` (and, from three cases up, a hand-written `GetValue(): object?`) - copy-pasted per arity, one more place to drift. Introduce a source-generator-backed convenience API instead:

- A new public attribute in `Interlocutor.CommonKernel` (zero-dependency, already home to `IOutcome`/`Outcome`, so any consumer can reference it without adding a dependency on `Interlocutor.Generator` itself) that marks a `partial union` declaration for member generation.
- A new `IIncrementalGenerator` in `Interlocutor.Generator` that, for any attributed `partial union`, emits a second partial file adding:
  - `Match<TResult>(Func<TCase1, TResult>, Func<TCase2, TResult>, ...)` - one delegate per case, compiler-enforced exhaustive, for projecting an outcome to a value.
  - `Switch(Action<TCase1>, Action<TCase2>, ...)` - the void-returning, side-effecting companion to `Match`.
  - A per-case, non-generic, overloaded `TryGetValue(out TCaseN value)` for every case type - matching the shape the union language feature's own pattern-matching already recognizes and prefers (avoids boxing on the value-type path), so this isn't inventing a new pattern, it's completing the one the compiler already looks for.
  - `override string ToString()` forwarding to `this.Value?.ToString()`, so the nice `ToString()` overrides already on every `ResultTypes` case type (`Conflict`, `NotFound`, `Failure`, ...) surface automatically instead of a default struct dump.
  - `IsSuccess` (replacing today's hand-written version, same semantics).
- The generator rejects (as a build error, not silently) any attributed union with fewer than two case types - a single-case union has no alternative for a `switch` to be exhaustive over, so it isn't a meaningful outcome.

Prove the whole mechanism end-to-end by converting the library's own most common shape, `Outcome<TSuccess, TFailure>`, from its current hand-written form to the attribute, deleting the hand-written `IsSuccess` it replaces. Leave every other arity (including the non-generic `Outcome`) untouched for now - that's Ticket 06.

**Blocked by:** Ticket 01 (needs the renamed `IOutcome`/`Outcome` in place first).

**Status:** done (already implemented)

- [x] The new attribute type exists in `Interlocutor.CommonKernel`, fully XML-documented, with no dependency added to that project.
- [x] The new `IIncrementalGenerator` exists in `Interlocutor.Generator` and emits `Match<TResult>`, `Switch`, per-case `TryGetValue`, `ToString()`, and `IsSuccess` for an attributed two-case `partial union`.
- [x] `Outcome<TSuccess, TFailure>` in `Interlocutor.CommonKernel` is converted to use the attribute; its hand-written `IsSuccess` is deleted (now generated); no other hand-written member is duplicated by the generator.
- [x] The generator emits a build error (not a silent no-op) when applied to an attributed union with fewer than two case types.
- [x] New tests in `Interlocutor.CommonKernel.Tests` (or a new generator-focused test project, whichever this repo's existing generator test coverage pattern uses) exercise `Match`, `Switch`, `TryGetValue` (both the hit and miss case per case type), and `ToString()` on `Outcome<TSuccess, TFailure>` for both the success and failure case.
- [x] `documentation/messages-and-result-types.md`'s `### Pattern Matching` section gains coverage of `Match`/`Switch`/`TryGetValue` alongside the existing raw-`switch`-on-`.Value` guidance (not a replacement for it - both remain valid).
- [x] `dotnet build` and `dotnet test` pass for the whole solution; `dotnet csharpier .` produces no diff.

## Comments

**2026-09-04** - Re-triaged while working the frontier: already implemented, and folded straight into the full-family form (Ticket 06's scope) rather than landing on just the two-case shape first - `GenerateOutcomeMembersAttribute` lives in `Interlocutor.CommonKernel` with no added dependency, `OutcomeMemberGenerator`/`OutcomeMemberEmitter` in `Interlocutor.Generator` emit the full member set, `INTERLOC011` fires as a build error for an attributed union with fewer than two cases, `OutcomeOfTwoCasesTests.cs` covers `Match`/`Switch`/`TryGetValue`/`ToString`, and `documentation/messages-and-result-types.md`'s Pattern Matching section documents all three alongside raw `switch`-on-`.Value`. No source changes were needed - Status flipped without a code commit.

