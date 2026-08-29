# 24 - S2325: make instance-independent test helper methods static

**What to build:** SonarAnalyzer's S2325 ("Make '<method>' a static method") fires roughly sixteen times, entirely within `tests/TodoTasks.Api.Tests` - helper methods like `CreateTaskAsync`/`AuthenticateAsANewUserAsync`/`Authenticate`/`CreateUserAsync` across several `Http/*EndpointTests.cs`/`*ControllerTests.cs` files that don't actually reference instance state (`this`). Confirm each one genuinely doesn't need instance context (it may take an `HttpClient`/factory as a parameter instead) before adding `static` - a helper that looks instance-independent today but is meant to evolve alongside per-test instance state should be left alone with a recorded reason instead.

**Blocked by:** None - can start immediately.

**Status:** ready-for-agent

- [ ] Every flagged helper method is either made `static` (confirmed it uses no instance state) or kept as an instance method with a recorded reason
- [ ] `dotnet build` reports zero `S2325` warnings
- [ ] `dotnet test` passes for `TodoTasks.Api.Tests`
