# 35 - S927: rename parameters to match their interface declaration

**What to build:** SonarAnalyzer's S927 ("Rename parameter '<name>' to '<interfaceName>' to match the interface declaration") fires roughly five times in `tests/Interlocutor.DependencyInjection.Microsoft.Tests/GeneratedRegistrationIntegrationTests.cs`, starting at line 142 - a test-double's implementing method uses a differently-named parameter (e.g. `cancellationToken`) than the interface it implements declares (e.g. `ct`). Rename each flagged parameter to match its interface's declared name exactly - a pure rename with no behavior change, improving IDE tooltip/tooling consistency for anyone implementing the same interface elsewhere in this codebase.

**Blocked by:** None - can start immediately.

**Status:** done

- [x] Every flagged parameter name matches its interface declaration exactly
- [x] `dotnet build` reports zero `S927` warnings
- [x] `dotnet test` passes for `Interlocutor.DependencyInjection.Microsoft.Tests`

## Comments

Renamed every flagged `cancellationToken` parameter to `ct` to match `IPipelineBehavior<,>.Handle`'s declared name - 5 sites total across `GeneratedRegistrationIntegrationTests.cs` (3, as this ticket named) and `ServiceCollectionExtensionsTests.cs` (2, per the actual `dotnet build` S927 output - this ticket's own prose attributed all "roughly five" occurrences to the first file alone, but the real split was 3/2 across both). Pure rename, no parameter used in any flagged method body.
