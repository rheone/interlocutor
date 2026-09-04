# 05 - Cross-assembly and compilation models get concise `DebuggerDisplay`

**What to build:** Stepping through cross-assembly module discovery, `ExternalModuleInfo`, `ExternalHandlerInfo`,
and `CompilationInfo` each show a concise summary of their key identifying fields in the debugger - the
referenced assembly/module name, the external handler's kind and request/response type names, or the
compilation's own namespace/reference facts - instead of a raw record dump, matching the treatment given to the
generator's other pipeline-model subsystems in Tickets 03 and 04.

**Blocked by:** Ticket 01.

**Status:** done

- [x] `ExternalModuleInfo`, `ExternalHandlerInfo`, and `CompilationInfo` each carry a `[DebuggerDisplay]` built
      from their own key identifying field(s) rather than a full property dump.
- [x] Every `[DebuggerDisplay]` added in this ticket is verified via Ticket 01's helper.
- [x] No change to generator output, emitted source, or these types' existing hand-rolled equality members -
      this ticket is debug-view-only.
- [x] `dotnet build` and `dotnet test` pass for the whole solution.

## Comments

**2026-09-04** - Re-triaged while working the frontier: already implemented in commit `9b27d74`. `ExternalModuleInfo`, `ExternalHandlerInfo`, and `CompilationInfo` each carry a `[DebuggerDisplay]` built from their own key fields, verified in `tests/Interlocutor.Generator.Tests/Models/CrossAssemblyModelsDebuggerDisplayTests.cs`. No source changes were needed this session.
