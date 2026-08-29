# ADR-0002: Pipeline Behavior Onion/Russian-Doll Model

**Status:** Accepted (amended)

## Context

Cross-cutting concerns - logging, auditing, validation, authorization, transactions - all need to run "around" every handler. Some do pre-work only, some post-work only, some both. The design needs one abstraction that can express "on the way in" and "on the way out" around a single invocation point.

## Decision

Use a single `IPipelineBehavior<TRequest,TResponse>` abstraction per concern, each wrapping a `next()` delegate in an onion/Russian-doll model (a specialization of Chain of Responsibility).

```csharp
public interface IPipelineBehavior<TRequest, TResponse>
{
    Task<TResponse> Handle(TRequest request, Func<Task<TResponse>> next, CancellationToken ct);
}
```

Behaviors are composed by the `Mediator` by walking `IEnumerable<IPipelineBehavior<TRequest,TResponse>>` from DI in reverse registration order - the first registered behavior wraps the outermost layer.

## Consequences

- **Positive:** One behavior class expresses both pre- and post-handler logic around a single `next()` call - no separate pre/post registries to keep in sync.
- **Positive:** Behaviors are generic over `<TRequest,TResponse>` and never inspect `T` - `Result<T>` flows through untouched.
- **Negative:** Execution order depends on DI registration order, which isn't guaranteed identical across containers. Mitigated by a pinned integration test.

## Alternatives considered

- **Separate `IPreProcessor`/`IPostProcessor` interfaces:** Rejected as an added abstraction with no benefit over a single wrapping delegate, and harder to guarantee pre/post run in matching pairs.
- **Aspect-oriented programming (post-sharp, source generators):** Rejected - adds build-time magic and reduces code transparency.

## Amendment (pipeline stages)

> **Note:** this amendment is implemented as shipped. `IPipelineStage<TRequest,TResponse>`'s `OnBeforeAsync` returns `StageResult<TResponse>` and supports the short-circuit contract described below.

The original decision rejected pre/post-processors *as a replacement for* behaviors. That rejection stands: a stage is not a second pipeline mechanism competing with `IPipelineBehavior<TRequest,TResponse>`, it is an ergonomic layer that composes *into* the one described above.

`IPipelineStage<TRequest,TResponse>` (`OnBeforeAsync`/`OnAfterAsync`) is hosted by a single `PipelineStageBehavior<TRequest,TResponse>` - an ordinary entry in the behavior list, positionable anywhere and registrable more than once. It aggregates every registered stage instance and runs their before-hooks in order, then the inner pipeline, then their after-hooks in reverse order. There is exactly one host behavior; stages never bypass it to wrap the chain directly.

**Short-circuit contract.** A before-hook may decline the request and return an outcome without the handler running. Because outcomes are value-type unions, a defaultable response cannot distinguish "did not short-circuit" from "short-circuited with the default value" - the before-hook therefore returns a dedicated two-state `StageResult<TResponse>` (`ShortCircuited` plus a `Response` meaningful only when `true`), rather than a nullable response:

```csharp
public readonly struct StageResult<TResponse>
{
    public bool ShortCircuited { get; }
    public TResponse Response { get; }   // meaningful only when ShortCircuited
}
// default(StageResult<T>) is Continue - consistent with the library's
// "safe at default value" convention used elsewhere in the result types.
```

On short-circuit: remaining before-hooks are skipped, the handler is skipped, and after-hooks run in reverse order for exactly the stages whose before-hook already ran - including the stage that short-circuited. Behaviors outside the stage host observe an ordinary response; they cannot tell a short-circuited stage result from a handled one. After-hooks do not run when the handler throws; an exception thrown by an after-hook is caught, logged at error level with the stage and request type, and execution continues (an opt-in switch escalates to rethrow) - pairing setup and teardown without letting a failing hook mask the real result.
