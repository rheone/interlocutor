# 03 - Remove two unreferenced `WellKnownTypeNames` constants in `Interlocutor.Generator`

**What to build:** `src/Interlocutor.Generator/WellKnownTypeNames.cs` declares two `public const string` metadata-name constants that nothing in the generator (or anywhere else in the solution) ever reads:

- `PipelineBehavior` (line 43, `"Interlocutor.Abstractions.Pipeline.IPipelineBehavior\`2"`) - a repo-wide search for `WellKnownTypeNames.PipelineBehavior` and for the bare identifier `PipelineBehavior` inside `Interlocutor.Generator` turns up only this declaration; no analyzer, emitter, or model builder resolves this symbol.
- `ValidationErrors` (line 89, `"Interlocutor.CommonKernel.ResultTypes.ValidationErrors"`) - same result: zero references anywhere. `ValidationShapeMismatchAnalyzer.cs` (the one place that reasons about validation-shaped failures) resolves `WellKnownTypeNames.ValidationFailureCase` (`IValidationFailureCase`) and checks `caseType.AllInterfaces.Contains(...)` instead (see `ValidationShapeMismatchAnalyzer.cs:215-216`) - i.e. the analyzer moved to an interface-marker check and this concrete-type constant was never removed after that.

Both compile fine as unreferenced `public const` fields since C# doesn't warn on unused public members, and neither showed up in a `dotnet format style`/`dotnet format analyzers` pass either (IDE0051/IDE0052 don't fire on `public` members). Delete both constants and their XML doc comments.

**Blocked by:** None - can start immediately

**Status:** done

- [x] `WellKnownTypeNames.PipelineBehavior` removed
- [x] `WellKnownTypeNames.ValidationErrors` removed
- [x] Repo-wide search confirms no remaining reference to either removed constant
- [x] `dotnet build` and `dotnet test tests/Interlocutor.Generator.Tests/Interlocutor.Generator.Tests.csproj` pass unchanged

## Comments

**2026-09-04** - Implemented: both constants and their XML doc comments removed from `WellKnownTypeNames.cs`. Repo-wide `rg` confirms zero remaining references. Full solution build (0 errors) and full test suite (1183/1183) pass unchanged; `Interlocutor.Generator.Tests` (128/128) unaffected.
