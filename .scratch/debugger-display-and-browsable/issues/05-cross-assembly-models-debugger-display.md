# 05 - Cross-assembly and compilation models get concise `DebuggerDisplay`

**What to build:** Stepping through cross-assembly module discovery, `ExternalModuleInfo`, `ExternalHandlerInfo`,
and `CompilationInfo` each show a concise summary of their key identifying fields in the debugger - the
referenced assembly/module name, the external handler's kind and request/response type names, or the
compilation's own namespace/reference facts - instead of a raw record dump, matching the treatment given to the
generator's other pipeline-model subsystems in Tickets 03 and 04.

**Blocked by:** Ticket 01.

**Status:** ready-for-agent

- [ ] `ExternalModuleInfo`, `ExternalHandlerInfo`, and `CompilationInfo` each carry a `[DebuggerDisplay]` built
      from their own key identifying field(s) rather than a full property dump.
- [ ] Every `[DebuggerDisplay]` added in this ticket is verified via Ticket 01's helper.
- [ ] No change to generator output, emitted source, or these types' existing hand-rolled equality members -
      this ticket is debug-view-only.
- [ ] `dotnet build` and `dotnet test` pass for the whole solution.
