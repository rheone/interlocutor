## Labels

ready-for-agent

## What to build

Implement `AuthorizationBehavior<TRequest, TResponse>` — a pipeline behavior that enforces declarative authorization policies on requests.

The `IRequiresPolicy` marker interface exists in `Mediance.Abstractions`:
```csharp
public interface IRequiresPolicy
{
    static abstract string Policy { get; }
}
```

The behavior should:
1. Check if `TRequest` implements `IRequiresPolicy`
2. If yes, resolve an authorization service (either `IAuthorizationService` from ASP.NET Core or a custom `IPolicyEvaluator` — decide which)
3. Evaluate the policy; if it fails, short-circuit with `Forbidden`
4. If the request doesn't implement `IRequiresPolicy`, pass through

Key design decision: whether to depend on ASP.NET Core's `IAuthorizationService` (requires `Microsoft.AspNetCore.App` framework reference) or define a minimal `IPolicyEvaluator` interface in Abstractions. Leaning toward the minimal interface approach to keep the abstractions package free of ASP.NET Core dependencies, with a separate bridge package for ASP.NET Core integration.

## Acceptance criteria

- [ ] `AuthorizationBehavior<TRequest, TResponse>` short-circuits with `Forbidden` when policy fails
- [ ] Requests without `IRequiresPolicy` pass through unchanged
- [ ] Works for both typed commands/queries and void `ICommand` requests (depends on Slice 08)
- [ ] The authorization abstraction is defined in a way that doesn't pull in ASP.NET Core dependencies
- [ ] Tests cover: allowed request, denied request, request without policy marker
- [ ] All existing tests still pass

## Blocked by

- 08-fix-void-command-dispatch (so void commands also receive authorization checks)
