# Mediance — Discovery & Expectations

**Date:** 2026-07-27

## Overview

This document captures discoveries from comparing the `bootstrapreference/` design materials against the actual `src/` and `tests/` implementation. It serves as a gap analysis, reconciliation log, and expectations baseline for the next implementation phase.

Bootstrap documents are **seed/design materials**, not specifications. Where source code disagrees with bootstrap, source code is authoritative — the bootstrap docs need updating (see PRD/ADR improvement tasks below).

## Implementation Status

### Implemented (8 source projects, 9 test projects)

| Package | Status | Notes |
|---------|--------|-------|
| `Mediance.CommonKernel` | Done | `Result<T>`, `Result`, `Failure`, `Success`, `PagedResult<T>` — C# 15 union types |
| `Mediance.Abstractions` | Done | Marker interfaces, handler contracts, `IPipelineBehavior`, `IMediator`, `Mediator`, `ResourceCommandHandler`, `StartupValidation` |
| `Mediance.AspNetCore` | Done | `ToActionResult()` mapping to `IActionResult` / ProblemDetails |
| `Mediance.DependencyInjection.Microsoft` | Done | Assembly-scanning `AddMediance()` for MS DI |
| `Mediance.DependencyInjection.Autofac` | Done | Assembly-scanning `AddMediance()` for Autofac |
| `Mediance.EntityFrameworkCore` | Done | `TransactionBehavior<TRequest,TResponse>` wrapping DbContext |
| `Mediance.NHibernate` | Done | `TransactionBehavior<TRequest,TResponse>` wrapping ISession |
| `Mediance.FluentValidation` | Done | `ValidationBehavior<TRequest,TResponse>` + `BulkItemValidationBehavior<TRequest,TItem,TResponseItem>` |

### Tests Status

| Area | Coverage | Notes |
|------|----------|-------|
| Result types pattern matching | Basic | 6 tests, coverage adequate |
| PagedResult pagination math | Good | 8 tests, edge cases covered |
| ToActionResult HTTP mapping | Good | 7 tests, all variants covered |
| ResourceCommandHandler flow | Basic | 3 tests (found, not-found, forbidden) |
| ValidationBehavior pipeline | Basic | 2 tests (pass-through, failure) |
| TransactionBehavior non-transactional | Smoke | 1 test each (EF + NH), only tests skip path |
| DI registration | Smoke | 1 test each, only checks IMediator is resolvable |
| Architecture dependency rules | Good | 4 tests, validates package dependency graph |
| **Mediator class itself** | **None** | Core dispatch logic untested |
| **Pipeline composition** | **None** | No integration tests for multi-behavior pipelines |
| **Cancellation propagation** | **None** | No test verifies CancellationToken forwarding |
| **Transactional flow** | **None** | No test verifies commit/rollback |
| **StartupValidation** | **None** | No tests |

## Bootstrap → Source Reconciliation

### Design decisions that changed during implementation

| Bootstrap Design | Actual Source | Rationale |
|-----------------|---------------|-----------|
| `TransactionBehavior<TRequest,TResponse,TContext>` (3 type params) | `TransactionBehavior<TRequest,TResponse>` (2 params, `Lazy<TContext>` via constructor) | Simpler DI registration — fits `IPipelineBehavior<,>` directly, no adapter class needed |
| `Forbidden` → `ObjectResult(ProblemDetails)` with body | `StatusCodeResult(403)` with empty body | Standard 403 behavior; reason is logged not returned |
| `BulkItemValidationBehavior<TRequest,TItem,TResponseItem>` doesn't fit `IPipelineBehavior<,>` | Actually fits: `IPipelineBehavior<TRequest, IReadOnlyList<TResponseItem>>` where `TRequest : IBulkCommand<TItem, TResponseItem>` | Compile-time constraint on TRequest ensures correct usage |
| Namespace: `Mediance` / `Mediance.Mediation` | `Mediance.CommonKernel` / `Mediance.Abstractions` | Better clarity; "Abstractions" is conventional in .NET ecosystem |
| `IRequiresPolicy` with `PolicyName` property | Same interface, but issue #10 ticket incorrectly states `static abstract string Policy` | Issue ticket needs correction |
| `Unauthorized` was dropped early in design | Confirmed absent in source | Authentication handled upstream (auth middleware) |

### Design elements correctly preserved

- All 5 Failure variants (ValidationFailed, RuleViolation, NotFound, Forbidden, Conflict)
- `Result<T>` / `Result` / `Failure` union types
- All marker interfaces (IQuery<T>, ICommand<T>, ICommand, ITransactionalRequest, IExternalStoreRequest, IAuditableRequest, IRequiresPolicy)
- Handler contracts with CancellationToken required (not defaulted)
- IPipelineBehavior with `next()` delegate pattern (Russian-doll/onion model)
- ResourceCommandHandler load-authorize-handle template method
- PagedResult with Items/Page/PageSize/TotalCount and computed TotalPages/HasNextPage
- Lazy<T> paired-marker session resolution
- Startup validation assembly scanning
- ResultExtensions HTTP mapping with ProblemDetails for all failure variants

## Known Gaps

### Critical (behavioral bugs)

| # | Gap | Ticket |
|---|-----|--------|
| 1 | `Mediator.Send(ICommand)` bypasses pipeline entirely — uses raw `MethodInfo.Invoke`, no behaviors run | 08 |
| 2 | `Mediator` class has zero unit tests — core dispatch logic untested | New |

### Missing Pipeline Behaviors

| # | Gap | Ticket |
|---|-----|--------|
| 3 | `LoggingBehavior<TRequest,TResponse>` — request type, handler, duration, outcome logging | 09 |
| 4 | `AuthorizationBehavior<TRequest,TResponse>` — policy-based access control via `IRequiresPolicy` | 10 |
| 5 | `AuditingBehavior<TRequest,TResponse>` — persisted audit trail via `IAuditSink` | New |
| 6 | `PipelineStage<TRequest,TResponse>` — generic ingress/egress hooks (described in bootstrap Conversation.md turn 11) | New |

### Missing Features

| # | Gap | Ticket |
|---|-----|--------|
| 7 | `INotification` / `Publish` — no event broadcasting mechanism | New |
| 8 | `ISender` / `IPublisher` split — interface segregation violation; `IMediator` combines both concerns | New |
| 9 | `IRequestContextAccessor` — no shared ambient context (correlation ID, current user, tenant) | New |
| 10 | Assembly scanning registration helper — no `AddMedianceMediation(params Assembly[])` convenience overload | New |
| 11 | JSON serialization for C# 15 union types — `Failure`, `Result<T>`, `Result` need custom `JsonConverter` | 05 |

### Naming & Consistency

| # | Gap | Ticket |
|---|-----|--------|
| 12 | Positional record parameters use `camelCase` (e.g., `PagedResult.items`), should be `PascalCase` per `.editorconfig` | 06 |
| 13 | `IRequiresPolicy` property named `PolicyName` in source but `Policy` in issue #10 ticket | 10 (fix in ticket body) |

### Transaction Completeness

| # | Gap | Ticket |
|---|-----|--------|
| 14 | EF Core `SaveChangesAsync` failure falls through to `DisposeAsync` rollback — should be explicit `try`/`catch` | 07 |
| 15 | `IExternalStoreRequest` marker exists but is never checked — should opt out of transaction even if `ITransactionalRequest` | 07 |
| 16 | NHibernate `Lazy<ISession>` resolution uses composite `&&` condition — should use guard clause (mirror EF Core style) | 03 |

### Documentation

| # | Gap | Ticket |
|---|-----|--------|
| 17 | README.md is 0 bytes — no project overview, quick-start, or package table | 02 |
| 18 | `CONTEXT.md` is a placeholder — no domain vocabulary | New |
| 19 | No ADRs recorded in `docs/adr/` — bootstrap `ADR-mediator-result-pipeline.md` exists but needs updating and proper placement | New |
| 20 | No PRD — bootstrap `PRD-mediator-result-pipeline.md` exists but needs reconciling with actual implementation | New |
| 21 | Package versions use floating patterns (`4.*`, `11.0.0-*`) — non-deterministic builds | 01 |

### Startup Validation

| # | Gap | Ticket |
|---|-----|--------|
| 22 | `MedianceStartupValidation` doesn't check `ICommand` (void) handler registrations | 04 |

## Bootstrap Document Quality Issues

These are issues within the bootstrap reference materials themselves that need fixing when PRD/ADRs are updated:

1. **PRD §9 (API boundary mapping):** Shows `Forbidden` → `ObjectResult(ProblemDetails)`. Actual implementation uses `StatusCodeResult(403)` (empty body). PRD should match source.

2. **ADR-0003 (Resource authorization):** States `ResourceCommandHandler` is a "concept, not an interface." Source implements it as an abstract class with template method. This is correct but should be clarified.

3. **Conversation.md turn 11:** Describes `PipelineStage<TRequest,TResponse>` as a generic ingress/egress hook. Not implemented in source. Should be noted as future feature or removed if superseded by `LoggingBehavior`.

4. **Addendum-B-library-packaging.md:** Lists package names `Mediance`, `Mediance.Mediation`, `Mediance.Mediation.EntityFrameworkCore` — actual names are `Mediance.CommonKernel`, `Mediance.Abstractions`, `Mediance.EntityFrameworkCore`. Also lists `Mediance.AspNetCore` which became `ResultExtensions` mapping only.

5. **Union Types.md:** This is a verbatim Microsoft Learn reference copy. Valid but may become stale as C# 15 union type syntax evolves toward GA.

6. **Abstractions.cs (bootstrap):** `IRequiresPolicy` has `PolicyName` but issue #10 ticket body describes it as `static abstract string Policy`. Ticket needs correction.

7. **CompositionAndUsage.cs (bootstrap):** Shows 8 pipeline behaviors registered but only 4 exist in actual source. Remaining 4 (Logging, Auditing, PagingValidation, Authorization) are gap tickets.

8. **Mediator.cs (bootstrap):** Shows line 141-151 void command using `GetMethod` + `Invoke` — same bug exists in actual source (ticket 08).

## Expectations for Next Phase

1. **Tickets capture issues for later resolution** — created before any code changes
2. **PRD and ADRs reconciled with source** — bootstrap docs updated to match actual implementation and extended for planned features
3. **Documentation gap closed** — CONTEXT.md populated, README written
4. **Bootstrap files remain in `bootstrapreference/`** — they serve as provenance/history, not active docs
