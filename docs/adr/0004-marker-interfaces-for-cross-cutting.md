# ADR-0004: Marker Interfaces for Cross-Cutting Opt-In

**Status:** Accepted

## Context

Not every request needs every cross-cutting concern. Queries don't need auditing or transactions. Many commands don't need a named authorization policy. Pipeline behaviors need a way to determine whether they apply to a given request without a central configuration list that's easy to forget to update.

## Decision

Use marker/capability interfaces declared on the request type itself. Each corresponding behavior checks `request is TMarker` and no-ops (calls `next()` directly) when absent.

| Interface | Opts into | Carries |
|-----------|-----------|---------|
| `ITransactionalRequest` | TransactionBehavior (EF Core / NHibernate) | — |
| `IExternalStoreRequest` | Declares "deliberately no SQL session" | — |
| `IAuditableRequest` | AuditingBehavior | `ActionName` (string) |
| `IRequiresPolicy` | AuthorizationBehavior | `PolicyName` (string) |

`ITransactionalRequest` and `IExternalStoreRequest` form a required pair — every `ICommand` should implement exactly one of them. `IExternalStoreRequest` exists as a positive declaration rather than relying on absence of `ITransactionalRequest`, which would be indistinguishable from "forgot to add the marker."

## Consequences

- **Positive:** Opt-in is visible at the request type's declaration — reading the `record` for a command tells you which concerns apply, no need to check pipeline registration.
- **Positive:** No reflection-based convention or naming-pattern guessing.
- **Negative:** Nothing compiler-enforces that the correct commands implement the correct markers. Mitigation: architecture tests (NetArchTest is a dependency) should assert all `ICommand` implementers also implement exactly one of `ITransactionalRequest` or `IExternalStoreRequest`.

## Alternatives considered

- **Central per-request-type configuration table:** Rejected — moves the opt-in decision away from the type definition, easy to drift out of sync.
- **Attribute-based opt-in:** Functionally equivalent but requires reflection to read attributes. Marker interfaces are checked with `is` pattern, which is faster and more idiomatic.
- **Implicit inference from absence of `ITransactionalRequest`:** Rejected — "forgot the marker" is indistinguishable from "deliberately no SQL."
