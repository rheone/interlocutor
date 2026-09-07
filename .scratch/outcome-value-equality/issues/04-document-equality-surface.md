# 04 - Document the new equality surface

**What to build:** The documentation set reflects that every `Outcome`/`Outcome<...>` arity now carries generated
value equality, and explains its two load-bearing semantics: union-type identity matters (two different case
types, or the "same" case value boxed into two different arities, are never equal), and item order never matters
for a collection-bearing case (`Failures`, `Conflicts`, `ValidationErrors`, and their `<TErrorCode>`/FluentValidation
counterparts) - results assembled from concurrent/async work in a different completion order are still the same
result. The order-independence explanation should describe the observable contract (order doesn't affect
equality; `Items` still preserves first-occurrence order for display) without needing to name or expose the
internal `DistinctItemSet<TItem>` type.

**Blocked by:** Ticket 01, Ticket 03 (needs both the aggregate-type semantics and the fully-migrated generator to
describe truthfully).

**Status:** done (already implemented)

- [x] `documentation/messages-and-result-types.md`'s generated-member list (currently "`Match`, `Switch`,
      per-case `TryGetValue`, `ToString`, and `IsSuccess`") is updated to include the new equality members, with
      a short explanation of the delegation and order-independence semantics above.
- [x] `documentation/glossary.md`'s `GenerateOutcomeMembersAttribute` entry is updated to mention the new members.
- [x] `documentation/extensibility.md`'s consumer custom-arity worked example (added in the prior
      `outcome-naming-and-arity-refactor` effort's Ticket 07) is updated to show the new members are part of the
      same generated surface a consumer's own attributed arity receives.
- [x] Every code example touched compiles against the shipped API, per this repo's documentation truthfulness
      rule.
- [x] `dotnet build` and `dotnet test` pass for the whole solution.

## Comments

**2026-09-04** - Re-triaged while working the frontier: already implemented. `documentation/messages-and-result-types.md`'s `## Outcome Unions` section lists the generated member set including "value equality (`Equals`, `GetHashCode`, `IEquatable<TSelf>`, `==`/`!=`)". `documentation/glossary.md`'s `GenerateOutcomeMembersAttribute` entry and `documentation/extensibility.md`'s custom-arity (`PaymentOutcome`) worked example both mention the same equality member set as part of the identical generated surface. No source changes were needed - Status flipped without a code commit.
