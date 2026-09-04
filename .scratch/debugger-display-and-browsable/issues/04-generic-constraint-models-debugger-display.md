# 04 - Generic-constraint and open-generic resolution models get concise `DebuggerDisplay`

**What to build:** Stepping through open-generic handler resolution and generic-constraint checking,
`OpenGenericHandlerModel`, `ClosedGenericRequestUsageModel`, `GenericConstraintSnapshot`,
`GenericParameterConstraintModel`, `ConstraintConversionFact`, `ResolvedArgumentTypeFact`, and
`TypeAnalysisResult` each show a concise summary of their key identifying fields in the debugger, instead of a
raw record dump - the same motivation as Ticket 03, applied to this generator subsystem's own equatable
snapshot types, several of which carry `ImmutableArray<...>` fields whose default display is just a type name.

**Blocked by:** Ticket 01.

**Status:** done

- [x] Each of the seven types listed above carries a `[DebuggerDisplay]` built from its own key identifying
      field(s) - the type argument or parameter name, the constraint outcome, or the display name it already
      computes for diagnostics - rather than a full property dump.
- [x] Every `[DebuggerDisplay]` added in this ticket is verified via Ticket 01's helper.
- [x] No change to generator output, emitted source, or the hand-rolled `Equals`/`GetHashCode` overrides these
      types already carry - this ticket is debug-view-only, additive alongside the existing equality members.
- [x] `dotnet build` and `dotnet test` pass for the whole solution.

## Comments

**2026-09-04** - Re-triaged while working the frontier: already implemented in commit `9b27d74`. All seven types (`OpenGenericHandlerModel`, `ClosedGenericRequestUsageModel`, `GenericConstraintSnapshot`, `GenericParameterConstraintModel`, `ConstraintConversionFact`, `ResolvedArgumentTypeFact`, `TypeAnalysisResult`) carry a `[DebuggerDisplay]` built from their own key fields, verified in `tests/Interlocutor.Generator.Tests/Models/GenericConstraintModelsDebuggerDisplayTests.cs`. Existing hand-rolled equality members untouched. No source changes were needed this session.
