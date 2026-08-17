# 09 - CancellationBehavior + ICancellable

## Labels

ready-for-agent

**What to build:** A new opt-in pipeline behavior that converts cooperative cancellation into the `Cancelled` outcome case. Add `ICancellable` as a marker interface (per ADR-0009's "nothing runs unless explicitly added" - no evaluator needed, since every request already carries its own `CancellationToken`). `CancellationBehavior<TRequest, TResponse>` (constrained `where TRequest : ICancellable, TResponse : IOperationOutcome`) catches `OperationCanceledException` from `next()`, filtered to only convert when `ct.IsCancellationRequested` is actually true (so it never swallows an unrelated exception), and constructs `Cancelled` via `IOutcomeCaseRegistry`.

**Blocked by:** 03 (IOutcomeCaseRegistry extension-point), 05 (new case types)

- [ ] `ICancellable` marker interface exists in `Interlocutor.Abstractions.Markers`
- [ ] `CancellationBehavior<TRequest, TResponse>` exists in `Interlocutor.Abstractions.Pipeline`, generic only over `TRequest, TResponse` (no extra type parameters, per the DI open-generic-resolution constraint documented on `AuthorizationBehavior`)
- [ ] It catches `OperationCanceledException` from `next()` only `when (ct.IsCancellationRequested)`, converting to `Cancelled` via `IOutcomeCaseRegistry`; any other exception (or a cancellation not tied to the ambient token) propagates unchanged
- [ ] Throws a clear exception (matching the `AuthorizationBehavior`/registry style) if `TResponse` has no `Cancelled` case
- [ ] Unit tests: cancellation mid-`next()` produces `Cancelled`; a non-cancellation exception still propagates; an unrelated `OperationCanceledException` (not tied to the ambient `ct`) still propagates
- [ ] `dotnet test tests/Interlocutor.Abstractions.Tests/Interlocutor.Abstractions.Tests.csproj` passes
