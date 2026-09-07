# 04 - Document the ConstructOrThrow fail-fast convention for custom pipeline behaviors

**What to build:** `documentation/extensibility.md` already covers "Registering Custom Behaviors" but says nothing about the fail-fast contract a custom generic behavior should follow when it needs to short-circuit with a specific outcome case it doesn't statically know `TResponse` has. Add a subsection documenting `OutcomeCaseRegistryExtensions.ConstructOrThrow` as the required pattern - construct via `IOutcomeCaseRegistry`, let a missing case surface as a named `InvalidOperationException` naming the behavior and the required case, never degrade to a null/silent value - cross-referencing ADR-0022 for the reasoning, and pointing to one of the shipped behaviors (e.g. `AuthorizationBehavior`) as a worked example.

**Blocked by:** 02 (cross-references ADR-0022)

**Status:** open

- [ ] `documentation/extensibility.md` gains a subsection on the fail-fast case-construction convention, in the existing "Registering Custom Behaviors" area
- [ ] Includes a compiling code example following `ConstructOrThrow`'s actual signature
- [ ] Cross-references ADR-0022 per the house style (no citation of `maintainer_docs/` needed for the rationale itself - state the reasoning in the doc, per `documentation/`'s own house style rule)
- [ ] Follows `documentation/`'s house style (no enumerated counts, glossary linking on first mention, no HTTP status codes outside `aspnetcore-integration.md`)
