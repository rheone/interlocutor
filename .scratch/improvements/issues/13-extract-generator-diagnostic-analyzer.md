# 13 - Extract the generator's diagnostic algorithms into a dedicated analyzer

**What to build:** `InterlocutorSourceGenerator`'s three diagnostic-reporting algorithms - `ReportMissingHandlers`, `ReportDuplicateHandlers`, and `ReportCatchAllNotificationHandlers` (~120 of the file's 348 lines) - move out of `InterlocutorSourceGenerator.cs` into a sibling analyzer type, called from `Execute` the same way `HandlerTypeAnalyzer.Analyze` already is. `InterlocutorSourceGenerator.cs` shrinks back to incremental-pipeline wiring (`Initialize`/`Execute`) plus calls out to analyzers, consistent with how `GenericUsageAnalyzer`, `OpenGenericHandlerResolver`, and `NotificationHandlerOrderPlanner` are already structured.

**Blocked by:** None - can start immediately

- [ ] The three `Report*` methods (and any private helpers they alone use) move to a new analyzer type in `src/Interlocutor.Generator/`
- [ ] `InterlocutorSourceGenerator.Execute` calls the new analyzer the same way it calls `HandlerTypeAnalyzer.Analyze`
- [ ] New unit tests call the analyzer directly (missing handler, duplicate handler, catch-all notification handler cases), independent of running the full generator pipeline
- [ ] All existing `Interlocutor.Generator.Tests` (including `GeneratorTestHarness.cs`-driven end-to-end cases) still pass, confirming diagnostics are unchanged
