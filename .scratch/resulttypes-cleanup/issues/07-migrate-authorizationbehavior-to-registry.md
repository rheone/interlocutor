# 07 - Migrate AuthorizationBehavior to IOutcomeCaseRegistry

## Labels

ready-for-agent

**What to build:** `AuthorizationBehavior<TRequest, TResponse>` currently constructs its `Forbidden` short-circuit case by calling `UnionCaseFactory` directly, which only works for compiler `union` types. Migrate it to take `IOutcomeCaseRegistry` as a constructor dependency and call `caseRegistry.Construct<TResponse, Forbidden>(...)` instead - this is the first real proof that the registry from ticket 03 is a drop-in replacement with no behavior regression, and the first behavior a hand-rolled `IOperationOutcome` type can now participate in.

**Blocked by:** 03 (IOutcomeCaseRegistry extension-point)

- [x] `AuthorizationBehavior<TRequest, TResponse>` takes `IOutcomeCaseRegistry` via constructor injection
- [x] The `Forbidden` short-circuit path calls `caseRegistry.Construct<TResponse, Forbidden>(...)` instead of calling `UnionCaseFactory` directly
- [x] The existing `InvalidOperationException` thrown when `TResponse` has no `Forbidden` case is preserved in behavior (now surfaced via `OutcomeCaseNotSupportedException` from the registry, or wrapped to preserve the existing message/exception type - confirm which against existing tests)
- [x] Existing `AuthorizationBehavior` tests still pass unchanged in observed behavior
- [x] A new test proves `AuthorizationBehavior` correctly short-circuits with `Forbidden` against a hand-rolled, non-union `IOperationOutcome` type registered with a custom `IOutcomeCaseRegistry`
- [x] `dotnet test tests/Interlocutor.Abstractions.Tests/Interlocutor.Abstractions.Tests.csproj` passes
