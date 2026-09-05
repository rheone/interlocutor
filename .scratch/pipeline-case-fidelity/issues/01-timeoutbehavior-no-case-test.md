# 01 - Add TimeoutBehavior's missing "no matching case" test

**What to build:** `AuthorizationBehaviorTests.cs`, `CancellationBehaviorTests.cs`, `RateLimitedBehaviorTests.cs`, `ValidationBehaviorTests.cs`, and `FluentValidationErrorsBehaviorTests.cs` each have a dedicated test asserting the fail-fast `InvalidOperationException` when a request opts into that behavior but its paired `TResponse` has no matching case to construct (e.g. `AuthorizationBehaviorTests`' "A request declaring a policy whose response has no `Forbidden` case at all" fixture). `TimeoutBehaviorTests.cs` is the one behavior test file of the six with no equivalent test, leaving `TimeoutBehavior`'s `ConstructOrThrow<TResponse, TCase>` fail-fast path unverified.

**Blocked by:** None - can start immediately

**Status:** ready-for-agent

- [ ] `TimeoutBehaviorTests.cs` gains a fixture request implementing `IHasTimeout` whose response type declares no timeout case, mirroring the sibling fixtures already used in `AuthorizationBehaviorTests`/`CancellationBehaviorTests`/`RateLimitedBehaviorTests`
- [ ] A test asserts `Assert.ThrowsAsync<InvalidOperationException>` when that fixture's request times out, matching the assertion style and exception-message expectations already used by the sibling behaviors' equivalent tests
- [ ] Existing `TimeoutBehaviorTests` all still pass unchanged
