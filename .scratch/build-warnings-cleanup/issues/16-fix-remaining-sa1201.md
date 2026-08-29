# 16 - Fix remaining SA1201 member-ordering warnings

**What to build:** Four `SA1201` warnings remain outside what tickets 02/04 already fixed:

- `tests/Interlocutor.Abstractions.Tests/AuditingBehaviorTests.cs` - "A field should not follow a method"
- `tests/Interlocutor.Abstractions.Tests/PipelineChainResolverTests.cs` - "A method should not follow a class"
- `tests/Interlocutor.CommonKernel.Tests/AggregateResultTypesTests.cs` - "A field should not follow a method"
- `tests/Interlocutor.DependencyInjection.Microsoft.Tests/ServiceCollectionExtensionsTests.cs` - "A field should not follow a method"

All are plain member-ordering violations. Reorder each to the position StyleCop's canonical order expects (the `csharp-code-organization` skill already agrees with StyleCop's default order in this repo, so no editorconfig change is needed), following the same fix pattern tickets 02/04 already used.

**Blocked by:** None - can start immediately.

**Status:** ready-for-agent

- [ ] `dotnet build` reports zero `SA1201` warnings for the four files listed above
- [ ] Only reordering changed - no logic, assertions, or public API shape changed
- [ ] `dotnet test` passes for the affected test projects
