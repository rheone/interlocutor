# ADR-0002: Pipeline Behavior Onion/Russian-Doll Model

**Status:** Accepted

## Context

Cross-cutting concerns — logging, auditing, validation, authorization, transactions — all need to run "around" every handler. Some do pre-work only, some post-work only, some both. The design needs one abstraction that can express "on the way in" and "on the way out" around a single invocation point.

## Decision

Use a single `IPipelineBehavior<TRequest,TResponse>` abstraction per concern, each wrapping a `next()` delegate in an onion/Russian-doll model (a specialization of Chain of Responsibility).

```csharp
public interface IPipelineBehavior<TRequest, TResponse>
{
    Task<Result<TResponse>> Handle(TRequest request, Func<Task<Result<TResponse>>> next, CancellationToken ct);
}
```

Behaviors are composed by the `Mediator` by walking `IEnumerable<IPipelineBehavior<TRequest,TResponse>>` from DI in reverse registration order — the first registered behavior wraps the outermost layer.

## Consequences

- **Positive:** One behavior class expresses both pre- and post-handler logic around a single `next()` call — no separate pre/post registries to keep in sync.
- **Positive:** Behaviors are generic over `<TRequest,TResponse>` and never inspect `T` — `Result<T>` flows through untouched.
- **Negative:** Execution order depends on DI registration order, which isn't guaranteed identical across containers. Mitigated by a pinned integration test.

## Alternatives considered

- **Separate `IPreProcessor`/`IPostProcessor` interfaces:** Rejected as an added abstraction with no benefit over a single wrapping delegate, and harder to guarantee pre/post run in matching pairs.
- **Aspect-oriented programming (post-sharp, source generators):** Rejected — adds build-time magic and reduces code transparency.
