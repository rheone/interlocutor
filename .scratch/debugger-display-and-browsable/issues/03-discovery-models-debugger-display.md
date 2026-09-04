# 03 - Discovery models get concise `DebuggerDisplay`

**What to build:** Stepping through the source generator's handler/request/outcome-union discovery, `HandlerModel`,
`RequestModel`, and `OutcomeUnionModel` each show a concise, human-readable summary in the debugger - built from
the display-name-shaped field(s) each already carries for its own diagnostics (e.g. handler/request type name,
kind, union display name) - instead of a raw record dump. The default record-synthesized `ToString()` these
types get today prints an `ImmutableArray<string>` field (`RunAfter`, `TypeParameterNames`,
`CaseTypeDisplayStrings`, and similar) as its runtime type name rather than its contents, and prints a
`Location` field's own verbose text in full - both unhelpful when scanning many of these while single-stepping.

**Blocked by:** Ticket 01.

**Status:** done

- [x] `HandlerModel`, `RequestModel`, and `OutcomeUnionModel` each carry a `[DebuggerDisplay]` built from their
      own existing identifying field(s) rather than a full property dump.
- [x] Every `[DebuggerDisplay]` added in this ticket is verified via Ticket 01's helper.
- [x] No change to generator output or emitted source - this ticket is debug-view-only. Existing generator tests
      (including `OutcomeMemberGeneratorTests`) pass unchanged.
- [x] `dotnet build` and `dotnet test` pass for the whole solution.

## Comments

**2026-09-04** - Re-triaged while working the frontier: already implemented in commit `9b27d74`. `HandlerModel`, `RequestModel`, and `OutcomeUnionModel` each carry a concise `[DebuggerDisplay]` built from their own identifying fields, verified in `tests/Interlocutor.Generator.Tests/Models/DiscoveryModelsDebuggerDisplayTests.cs`. No generator output/emitted-source change - debug-view-only. No source changes were needed this session.
