# 24 - S2325: make instance-independent test helper methods static

**What to build:** SonarAnalyzer's S2325 ("Make '<method>' a static method") fires roughly sixteen times, entirely within `tests/TodoTasks.Api.Tests` - helper methods like `CreateTaskAsync`/`AuthenticateAsANewUserAsync`/`Authenticate`/`CreateUserAsync` across several `Http/*EndpointTests.cs`/`*ControllerTests.cs` files that don't actually reference instance state (`this`). Confirm each one genuinely doesn't need instance context (it may take an `HttpClient`/factory as a parameter instead) before adding `static` - a helper that looks instance-independent today but is meant to evolve alongside per-test instance state should be left alone with a recorded reason instead.

**Blocked by:** None - can start immediately.

**Status:** done

- [x] Every flagged helper method is either made `static` (confirmed it uses no instance state) or kept as an instance method with a recorded reason
- [x] `dotnet build` reports zero `S2325` warnings
- [x] `dotnet test` passes for `TodoTasks.Api.Tests`

## Comments

Confirmed each of the 16 flagged helpers (`AuthenticateAsANewUserAsync`, `CreateTaskAsync`, `CreateUserAsync`, `Authenticate`, `CreateTaskAndReturnItsOwnerIdAsync`) across 9 `TodoTasks.Api.Tests/Http/*Tests.cs` files uses no instance state - all reference only their own parameters and static fields (`WebJsonOptions`). Made all `static`, then removed the now-illegal `this.` qualifier from every call site (the compiler's own CS0176 caught every one); one file (`CompleteReopenTodoTaskEndpointTests.cs`) needed a second, previously-unflagged helper (`CreateTaskAsync`) made `static` too, since it only referenced the newly-static sibling via `this.`.
