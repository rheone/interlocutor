> **Status: superseded** — this issue predates the common-kernel migration.
> Reason: implemented; see remediation 07 for the remaining hook-pairing defect.
> Superseded by the effort in `.scratch/remediation/`. Do not pick this up.

## Labels

ready-for-agent

## What to build

Implement `PipelineStage<TRequest, TResponse>` — a generic ingress/egress hook that allows consumers to execute code before and after every request passes through the pipeline, without creating a full `IPipelineBehavior<TRequest, TResponse>` implementation.

The concept was described in the bootstrap conversation (turn 11) as a lighter-weight alternative to `IPipelineBehavior` for cases where the consumer only needs pre/post hooks (e.g., timing, metrics, correlation ID injection).

Proposed interface:

```csharp
public interface IPipelineStage<TRequest, TResponse>
{
    Task OnBeforeAsync(TRequest request, CancellationToken ct);
    Task OnAfterAsync(TRequest request, Result<TResponse> result, CancellationToken ct);
}
```

The `Mediator` would resolve all registered `IPipelineStage<TRequest, TResponse>` instances and invoke them around each handler call.

Key design decisions:
- Should this be a separate interface or is this adequately covered by `IPipelineBehavior` and the proposed `LoggingBehavior`?
- If implemented, should stages run inside or outside the behavior chain?
- Should stages be composable or independently registered?

Consider whether implementing `LoggingBehavior` (ticket 09) as an `IPipelineBehavior` makes this unnecessary. Revisit after ticket 09 is complete.

## Acceptance criteria

- [ ] Decision documented (either implement or explicitly reject in favor of `IPipelineBehavior` pattern)
- [ ] If implemented: `IPipelineStage<TRequest,TResponse>` is defined, Mediator invokes stages around handler execution
- [ ] If rejected: ADR records why `IPipelineBehavior` supersedes it
- [ ] All existing tests still pass

## Blocked by

- 09-logging-behavior (to assess overlap)
