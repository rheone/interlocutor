# 29 - S3878: pass NetArchTest arguments directly instead of wrapping them in an array

**What to build:** SonarAnalyzer's S3878 ("Remove this array creation and simply pass the elements") fires five times in `tests/Interlocutor.Architecture.Tests/DependencyTests.cs`, starting at line 61 - calls building an inline array (likely for a `params`-compatible NetArchTest assertion method) that could instead pass the elements directly as individual arguments. Confirm the target method actually has a `params` overload before mechanically unwrapping each call - if it doesn't, this may be a different shape SonarAnalyzer is mis-flagging, in which case suppress with that noted instead.

**Blocked by:** None - can start immediately.

**Status:** ready-for-agent

- [ ] Every flagged array-creation call is simplified to pass its elements directly (confirmed a `params`-compatible overload exists) or kept with a recorded reason
- [ ] `dotnet build` reports zero `S3878` warnings
- [ ] `dotnet test` passes for `Interlocutor.Architecture.Tests`
