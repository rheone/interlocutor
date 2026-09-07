# ADR-0024: Defer a Custom "Populate Switch" Fixer for `union` Types

**Status:** Accepted

> [!NOTE]
> **Numbering note.** This ADR claims 0024 as the next number available in `maintainer_docs/adr/` at the time it was written (highest existing file was 0023).

## Context

A raw `switch` over an `Outcome<...>` case (see [ADR-0001](0001-use-csharp-15-unions-for-result-types.md)) that omits a case is flagged by the compiler as `CS8509` ("the switch expression does not handle all possible values of its input type"). For an ordinary closed type - an enum, a sealed record hierarchy - Roslyn's shipped IDE fixer (`IDE0072` for switch expressions, `IDE0010` for switch statements, commonly surfaced as the "Populate switch" lightbulb) already offers to insert the missing arms automatically. The idea considered here was building an Interlocutor-specific analog: a `CodeFixProvider` in `Interlocutor.Generator` that recognizes an incomplete switch over a `union`-declared `Outcome<...>` and fills in the missing case arms.

Before scoping that work, the actual behavior was verified hands-on against the pinned preview SDK (`11.0.100-preview.6.26359.118`, per `global.json`) rather than assumed:

- Switching directly on the union value itself (`result switch { ... }`) already produces a `CS8509` warning that **names the specific missing case type** (e.g. `"the pattern 'Interlocutor.CommonKernel.ResultTypes.NotFound' is not covered"`) - the compiler's own exhaustiveness analysis already understands a `union` declaration's closed case set.
- Switching on `.Value` (the `IUnion` interface member, statically typed `object?`) instead produces a generic `CS8509` warning that names only the missing discard pattern (`'_' is not covered`) - `.Value`'s declared type carries no case-set information for the compiler to name specific missing types from.
- Running `dotnet format analyzers --diagnostics IDE0072` against both forms fired **zero diagnostics** in either case - the shipped "Populate switch" fixer does not yet recognize `union` types at all in this SDK preview, even where the compiler-side exhaustiveness diagnostic already does.
- No `CodeFixProvider` exists anywhere in this repository today; a custom fixer would be new analyzer-package infrastructure, not an extension of an existing one.
- `Interlocutor.Generator` is referenced by `Interlocutor.CommonKernel` with `IncludeBuildOutput=false` and does not flow downstream to consumers - a consumer only gets Generator's analyzers by referencing the package directly, which most don't (`TutorialProject` uses runtime `ScanAssembly` discovery instead). A fixer bundled into `Interlocutor.Generator` would miss most of the audience a "fill in my switch" convenience is actually for.

## Decision

**Do not build a custom switch-populate analyzer/codefix.** This is upstream compiler/IDE tooling territory, not a gap in Interlocutor's own type or pipeline design - the compiler's exhaustiveness analysis for `union` types is already correct (as the direct-on-union CS8509 message above shows); only the IDE-side auto-fill lightbulb hasn't caught up yet, and that catch-up belongs to Roslyn, not to a library that merely uses the `union` language feature. Building and maintaining a parallel `CodeFixProvider` to paper over a preview-tooling gap risks becoming permanent maintenance burden or outright redundant the moment the shipped fixer adds `union` support - and duplicating IDE tooling is a poor use of a mediator library's own analyzer surface (`INTERLOC0xx` today covers handler discovery, case fidelity, and validation shape - all Interlocutor-specific concerns; a generic switch-fill fixer is not).

Instead:

- **Document the gap** where a reader would hit it - the "Pattern Matching" section of [messages-and-result-types.md](../../documentation/messages-and-result-types.md#pattern-matching) now notes that `IDE0072`/`IDE0010` don't yet recognize `union` types, and that `Match`/`Switch` remain exhaustive by construction (a missing case is a missing delegate argument, not a warning to overlook) as the unaffected alternative in the meantime.
- **Correct an adjacent, unrelated inaccuracy found during this investigation while touching the same prose**: the docs previously described an incomplete switch as a "❌ Compile error." By default it is only a `CS8509` warning - `TreatWarningsAsErrors` is commented out in `Directory.Build.props`, and nothing in this project or a fresh consumer project escalates it otherwise. Both `messages-and-result-types.md` and `beginners-guide-todo-api.md`'s manual-mapping example are corrected to say so.
- **Track revisiting this** as `.scratch/outcome-switch-populate-fixer/issues/01-track-upstream-populate-switch-support.md`, `needs-info` - the trigger to reopen is a released (non-preview) C# toolchain shipping `IDE0072`/`IDE0010` support for `union` types, which isn't something to act on yet but shouldn't be silently forgotten either.

## Alternatives considered

- **Build the fixer now, scoped to switches on the union value directly** (where the compiler already names the exact missing case, making the fixer's own case-detection trivial - just read the diagnostic's own message/location data rather than re-deriving the case set from the declared `Outcome<...>` generic arguments). Rejected for now: still new `CodeFixProvider` infrastructure to build and maintain against a preview-only compiler surface that may itself change before .NET 11 GA, for a gap that reads as strictly upstream's to close.
- **Ship it as a new standalone analyzer-only package** (rather than bundling into `Interlocutor.Generator`), so it reaches any consumer regardless of dispatch strategy. This would have been the right packaging *if* the fixer were built - noted here so a future revisit doesn't have to re-derive it - but doesn't change the "not now" decision above.
- **Promote `Match`/`Switch` harder instead of fixing raw `switch` at all.** Rejected as a replacement for documenting the gap (though it's already the standing recommendation): plenty of consumers reasonably prefer a raw `switch` for readability or familiarity, and the docs already sanction it as a first-class idiom - the gap is real for that idiom and worth naming, not worth arguing people out of.

## Consequences

- **Positive:** No new analyzer-package surface to design, build, or maintain against a preview language feature that may still change before GA.
- **Positive:** The two doc corrections (CS8509 severity; the new IDE0072/IDE0010 caveat) leave `documentation/` accurate against the actually-shipped SDK behavior, verified rather than assumed.
- **Neutral:** A consumer relying on a raw `switch` over `.Value` gets no IDE assist filling in missed cases until Roslyn's own fixer adds `union` support - `Match`/`Switch` remain the documented, exhaustive-by-construction alternative for anyone who wants that guarantee today.
- **Negative:** This gap could persist indefinitely if upstream never prioritizes `union` support in `IDE0072`/`IDE0010` - `.scratch/outcome-switch-populate-fixer/issues/01-track-upstream-populate-switch-support.md` exists so that risk is at least tracked, not silently accepted.
