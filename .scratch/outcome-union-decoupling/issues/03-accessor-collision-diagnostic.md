# 03 - Build-time diagnostic (INTERLOC012) for colliding case accessor names

**What to build:** declaring a `[GenerateOutcomeMembers]` union where two case types would produce the same `TryGet{TypeName}` accessor name must fail the build by default with a new diagnostic, rather than silently emitting ambiguous or non-compiling generated code. Three collision shapes to catch:

- Two case types with the same simple name from different namespaces (e.g. `MyApp.Http.NotFound` and `MyApp.Domain.NotFound` as peer cases).
- Two closed instantiations of the same open generic case type used as peer cases (e.g. `ValidationError<string>` and `ValidationError<int>` would both want `TryGetValidationError`).
- A case type's name colliding with a reserved generated member name (`Value`, `Match`, `Switch`, `ToString`, `Equals`, `GetHashCode`, `TryGetValue`, `IsSuccess`, `ActiveCaseType`).

Same posture as the existing duplicate-case-type diagnostic (`INTERLOC009`): default severity error, reported at the union declaration site, configurable via `dotnet_diagnostic.INTERLOC012.severity`.

**Blocked by:** 02 - needs the accessor-name computation introduced there to have something to detect collisions against

**Status:** done

- [x] Two case types with the same simple name (different namespaces) in one union declaration reports `INTERLOC012` at the union declaration site.
- [x] Two closed instantiations of the same open generic case type as peer cases reports `INTERLOC012`.
- [x] A case type whose name would collide with a reserved generated member name reports `INTERLOC012`.
- [x] `INTERLOC012` defaults to error severity and is configurable the same way as the other `INTERLOC0xx` diagnostics.
- [x] A union with no colliding accessor names compiles cleanly with no `INTERLOC012` reported, and still gets the named accessors from ticket 02.
- [x] `documentation/extensibility.md` gets a diagnostic doc block for `INTERLOC012`, matching the existing `INTERLOC009` block's format.

## Comments

New `OutcomeAccessorCollisionAnalyzer` (plain reporting helper driven by an already-built `OutcomeUnionModel`, called from `OutcomeMemberGenerator.Execute` - mirrors `HandlerDiagnosticAnalyzer`'s shape, not a standalone `DiagnosticAnalyzer`, since the check is naturally keyed off ticket 02's `CaseAccessorNames`). Two independent passes over `CaseAccessorNames`: group by accessor name and flag any name shared by 2+ positions (catches both the same-simple-name-different-namespace shape and the two-closed-instantiations-of-one-open-generic shape - both reduce to "two peer cases produce the same simple name"); separately, flag any single case whose name is itself one of the nine reserved generated member names. Both report the same `INTERLOC012` descriptor at the union's declaration site, with a reason clause distinguishing which shape fired. A collision skips emitting the generated partial declaration entirely (mirroring `INTERLOC011`'s existing precedent) rather than letting the compiler's own `CS0121`/`CS0111` stand in for the clearer diagnostic.

Chose error as the default severity per this ticket's own acceptance criteria, even though `INTERLOC009` (the diagnostic named as "same posture") defaults to warning - `INTERLOC011` (also `Interlocutor.Outcomes`, also reported at the declaration site for an attributed union with no meaningful member set to fall back to) is the closer precedent for severity, and matches "must fail the build by default" in this ticket's own opening sentence.

Four new tests in `OutcomeMemberGeneratorTests.cs` cover all three collision shapes plus a no-collision control case (asserting no diagnostic and that the named accessors from ticket 02 are still emitted). Full `Interlocutor.Generator.Tests` (106/106) and `Interlocutor.CommonKernel.Tests` (265/265) pass; whole-solution `dotnet build` is clean.
