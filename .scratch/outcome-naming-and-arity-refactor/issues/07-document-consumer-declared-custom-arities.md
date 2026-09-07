# 07 - Document and demonstrate consumer-declared custom arities

**What to build:** The attribute/generator introduced in Ticket 05 and applied to the whole shipped family in Ticket 06 isn't limited to types this library ships - any consumer can decorate their own `partial union` with the same attribute and get the same `Match`/`Switch`/`TryGetValue`/`ToString`/`IsSuccess` member set, including for an arity beyond the shipped 2-6 range (7+, or a non-default case ordering), with the same duplicate-case-type analyzer (Ticket 03) coverage. Nothing beyond referencing `Interlocutor.CommonKernel` (which every consumer already does) is required.

Add a worked example to `documentation/extensibility.md`, alongside its existing "custom `IOutcome` implementation" and "custom failure-case marker" sections, showing a consumer declaring their own arity - e.g. a seven-case outcome for an operation this library's shipped family genuinely can't express - end to end: the attributed `partial union` declaration, the generated members it gets for free, and a short note on why the shipped family stops at six (per `documentation/messages-and-result-types.md`'s `## Choosing Arities` guidance: an operation needing more cases than that is usually a sign it's trying to do too much, not that the type needs to grow) rather than implying seven-plus is a normal everyday choice.

**Blocked by:** Ticket 05, Ticket 06 (the worked example should describe a mechanism proven on the library's own fully-migrated family, not a partially-generated one).

**Status:** done (already implemented)

- [x] `documentation/extensibility.md` gains a worked example of a consumer declaring a custom-arity outcome type via the public attribute, including the full generated member set it receives.
- [x] The example explicitly frames this as an escape hatch for a genuine outlier, cross-referencing `## Choosing Arities` in `documentation/messages-and-result-types.md` rather than encouraging routine use of arities beyond the shipped 2-6 range.
- [x] `documentation/index.md`'s extensibility-map entry is updated if needed to mention custom-arity declarations alongside the other listed extension points.
- [x] `documentation/glossary.md` gains an entry for the new attribute type if one doesn't already cover it.
- [x] The worked example is a real, compiling code sample (per this repo's documentation truthfulness rule) - either lives as an actual test/sample in the repo that the doc excerpt is drawn from, or is verified to compile against the shipped API before being committed.
- [x] `dotnet build` and `dotnet test` pass for the whole solution (no production source changes expected beyond whatever backs the compiling sample).

## Comments

**2026-09-04** - Re-triaged while working the frontier: already implemented. `documentation/extensibility.md` § "Custom-arity outcome unions: `GenerateOutcomeMembersAttribute`" walks through a seven-case `PaymentOutcome` union backed by real, compiling code in `tests/Interlocutor.CommonKernel.Tests/CustomArityOutcomeTests.cs` (the test file's own doc comment says the extensibility.md excerpt is drawn from it), and frames it as an outlier escape hatch rather than routine use. `documentation/index.md`'s extensibility map lists "custom-arity outcome unions via `GenerateOutcomeMembersAttribute`". `documentation/glossary.md` has a `GenerateOutcomeMembersAttribute` entry. No source changes were needed - Status flipped without a code commit.

