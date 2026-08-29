# 15 - Add SonarAnalyzer.CSharp; fix S2699; document S326 as not-applicable

**What to build:** No `SonarAnalyzer` package is referenced anywhere in the solution today, so Sonar-sourced diagnostics (`S2699`, `S326`, and others seen only via an IDE-only SonarLint pass per ticket 05) have no CLI source of truth. Add `SonarAnalyzer.CSharp` (pinned exact version, `PrivateAssets="all"`, matching how `StyleCop.Analyzers`/`Roslynator.*` are referenced in `Directory.Build.props`) with default severities. Fix every genuine `S2699` ("tests should include assertions") finding the way ticket 05 already modeled for its manual review - a real assertion added, or a recorded reason (helper-method delegation, mocking-framework verification call, a deliberate must-not-throw test). Confirm and document that `S326` cannot fire under any configuration - it's a reserved Sonar rule ID with no C# implementation in `SonarAnalyzer.CSharp`, at any version, verified against the analyzer's own rule-mapping source. Any other rule newly activated by adding the package is out of scope here - file each as its own follow-up `.scratch/build-warnings-cleanup` ticket rather than fixing it inline.

**Blocked by:** None - can start immediately.

**Status:** ready-for-agent

- [ ] `SonarAnalyzer.CSharp` is referenced solution-wide via `Directory.Build.props`, pinned to an exact version, `PrivateAssets="all"`
- [ ] `dotnet build` reports zero `S2699` warnings, with every fix adding a real assertion (not a mechanical no-op to silence the rule)
- [ ] A comment/note records why `S326` is closed as not-applicable (no C# implementation exists)
- [ ] Every other newly-surfaced Sonar rule finding is filed as a new `ready-for-agent` ticket under `.scratch/build-warnings-cleanup/issues/`, not fixed in this ticket
- [ ] `dotnet test` passes solution-wide
