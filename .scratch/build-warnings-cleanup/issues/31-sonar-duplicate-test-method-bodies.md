# 31 - S4144: two test methods have identical implementations

**What to build:** SonarAnalyzer's S4144 ("Update this method so that its implementation is not identical to '<other method>'") fires on `tests/Interlocutor.DependencyInjection.Abstractions.Tests/AssemblyTypeScannerTests.cs(84,17)` (flagged against `ExtractLoadedTypes_ReturnsTheTypesThatLoaded_ContinuingPastTheOnesThatDidNot`) and one occurrence in `tests/Interlocutor.DependencyInjection.Microsoft.Tests`. Read both methods in each pair: if they're genuinely testing the same thing twice (accidental duplication), delete the redundant one; if they're deliberately proving the same behavior through two different entry points (e.g. one via the builder API, one via a lower-level call), keep both but make the duplication self-evidently intentional - e.g. a shared private helper the two thin test bodies both call, with each test's own name documenting which entry point it exercises.

**Blocked by:** None - can start immediately.

**Status:** ready-for-agent

- [ ] Each flagged pair is resolved: the genuinely-redundant test removed, or the intentional duplication refactored so a shared helper makes the relationship obvious rather than two copy-pasted bodies
- [ ] `dotnet build` reports zero `S4144` warnings
- [ ] `dotnet test` passes for both affected test projects, with no loss of actual coverage
