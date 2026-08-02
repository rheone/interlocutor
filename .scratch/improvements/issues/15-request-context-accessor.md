> **Status: superseded** — this issue predates the common-kernel migration.
> Reason: interface and context record exist; remaining work is remediation 12.
> Superseded by the effort in `.scratch/remediation/`. Do not pick this up.

## Labels

ready-for-agent

## What to build

Implement `IRequestContextAccessor` — a shared ambient context that carries correlation IDs, current user identity, and tenant information through the request pipeline.

This addresses a recurring pattern in the examples: each handler and behavior currently defines its own `ICurrentUser` (see `CompositionAndUsage.cs` from bootstrap). A centralized accessor eliminates the duplication and provides a single seam for infrastructure (auditing, logging, authorization).

Proposed interface:

```csharp
public interface IRequestContextAccessor
{
    RequestContext? Context { get; set; }
}

public sealed record RequestContext
{
    public string CorrelationId { get; init; } = Guid.NewGuid().ToString();
    public string? UserId { get; init; }
    public string? TenantId { get; init; }
    public IReadOnlyDictionary<string, object>? Items { get; init; }
}
```

Key design decisions:
- **Lifetime**: Should be `Scoped` (one per request/operation scope)
- **Population**: An ASP.NET Core middleware should populate it from `HttpContext` (correlation header, authenticated user, tenant header)
- **Null-safe**: If no middleware populates it, `Context` is null — consumers must handle gracefully
- **Thread-safe**: `RequestContext` is immutable (record), swap via `Context { get; set; }`

Package placement:
- Interfaces in `Mediance.Abstractions` (or a new `Mediance.Abstractions` file)
- Default implementation in `Mediance.Abstractions`
- ASP.NET Core middleware in `Mediance.AspNetCore`

## Acceptance criteria

- [ ] `IRequestContextAccessor` defined in `Mediance.Abstractions`
- [ ] Default scoped implementation provided
- [ ] ASP.NET Core middleware populates from `HttpContext`
- [ ] Tests cover: null context, populated context, middleware population
- [ ] All existing tests still pass

## Blocked by

None — can start immediately
