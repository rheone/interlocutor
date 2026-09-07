# 04 - Document the ConstructOrThrow fail-fast convention for custom pipeline behaviors

**What to build:** `documentation/extensibility.md` already covers "Registering Custom Behaviors" but says nothing about the fail-fast contract a custom generic behavior should follow when it needs to short-circuit with a specific outcome case it doesn't statically know `TResponse` has. Add a subsection documenting `OutcomeCaseRegistryExtensions.ConstructOrThrow` as the required pattern - construct via `IOutcomeCaseRegistry`, let a missing case surface as a named `InvalidOperationException` naming the behavior and the required case, never degrade to a null/silent value - cross-referencing ADR-0022 for the reasoning, and pointing to one of the shipped behaviors (e.g. `AuthorizationBehavior`) as a worked example.

**Blocked by:** 02 (cross-references ADR-0022)

**Status:** done - implemented in `66b3853`. Two corrections to this ticket's own stale premises: cross-references **ADR-0023** ("Pipeline Behavior Case Construction Fails Fast, Never Degrades or Reshapes"), not ADR-0022 - that number was claimed by the unrelated outcome-classifier-retirement PRD first, bumping this ADR to 0023. Uses `CancellationBehavior` as the worked example, not `AuthorizationBehavior` - the latter has since migrated to a separate `Eject.WithCase` mechanism and no longer calls `ConstructOrThrow` directly (the new subsection notes this explicitly so a reader checking that source isn't confused).

- [x] `documentation/extensibility.md` gains a subsection on the fail-fast case-construction convention, placed near the existing custom-behavior material (see status note - `extensibility.md` itself has no literal "Registering Custom Behaviors" heading; that section lives in `pipeline-and-behaviors.md`, which `extensibility.md` links to)
- [x] Includes a compiling code example following `ConstructOrThrow`'s actual signature (verified verbatim against `src/Interlocutor.Abstractions/Pipeline/Cases/OutcomeCaseRegistryExtensions.cs` and `CancellationBehavior.cs`)
- [x] Cross-references the correct current ADR (0023, not the ticket's stale 0022) per house style - no citation of `maintainer_docs/`, reasoning stated as fact in the doc
- [x] Follows `documentation/`'s house style (no enumerated counts, glossary linking on first mention, no HTTP status codes outside `aspnetcore-integration.md`)
