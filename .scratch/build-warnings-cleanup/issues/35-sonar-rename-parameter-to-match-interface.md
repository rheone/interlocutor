# 35 - S927: rename parameters to match their interface declaration

**What to build:** SonarAnalyzer's S927 ("Rename parameter '<name>' to '<interfaceName>' to match the interface declaration") fires roughly five times in `tests/Interlocutor.DependencyInjection.Microsoft.Tests/GeneratedRegistrationIntegrationTests.cs`, starting at line 142 - a test-double's implementing method uses a differently-named parameter (e.g. `cancellationToken`) than the interface it implements declares (e.g. `ct`). Rename each flagged parameter to match its interface's declared name exactly - a pure rename with no behavior change, improving IDE tooltip/tooling consistency for anyone implementing the same interface elsewhere in this codebase.

**Blocked by:** None - can start immediately.

**Status:** ready-for-agent

- [ ] Every flagged parameter name matches its interface declaration exactly
- [ ] `dotnet build` reports zero `S927` warnings
- [ ] `dotnet test` passes for `Interlocutor.DependencyInjection.Microsoft.Tests`
