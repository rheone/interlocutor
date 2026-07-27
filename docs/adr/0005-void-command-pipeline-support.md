# ADR-0005: Void Command Pipeline Support

**Status:** Accepted

## Context

Commands come in two flavors: typed commands (`ICommand<TResponse>`) that return `Result<TResponse>`, and void commands (`ICommand`) that return `Result` (non-generic). The pipeline behavior interface is generic over `<TRequest,TResponse>`, which means it naturally works with `IPipelineBehavior<TCommand, TResponse>` for typed commands. Void commands need the same pipeline treatment — logging, validation, authorization, transactions — but produce `Result` instead of `Result<TResponse>`.

## Decision

Void commands dispatch through the same pipeline mechanism as typed commands via `VoidCommandHandlerWrapper<TCommand>`. The wrapper:

1. Resolves `ICommandHandler<TCommand>` from DI.
2. Wraps the handler's `Task<Result>` return into `Task<Result<Result>>` — using `Result` as the pipeline's `TResponse`.
3. Chains all registered `IPipelineBehavior<TCommand, Result>` behaviors in reverse order around it.
4. Unwraps the final `Result<Result>` back to `Result` for the caller.

```csharp
public override async Task<Result> Handle(object request, IServiceProvider services, CancellationToken ct)
{
    Func<Task<Result<Result>>> pipeline = async () =>
    {
        var r = await handler.Handle(typedRequest, ct);
        return r.Value switch
        {
            Success s => new Result<Result>(s),
            Failure f => new Result<Result>(f),
            _ => throw new UnreachableException(),
        };
    };

    foreach (var behavior in services.GetServices<IPipelineBehavior<TCommand, Result>>().Reverse())
    {
        var next = pipeline;
        pipeline = () => behavior.Handle(typedRequest, next, ct);
    }

    var pipelineResult = await pipeline();
    return pipelineResult.Value switch
    {
        Result r => r,
        Failure f => new Result(f),
        _ => throw new UnreachableException(),
    };
}
```

## Consequences

- **Positive:** Void commands get full pipeline treatment (validation, transaction, authorization, logging) with no behavior changes — every `IPipelineBehavior<,>` is already generic over `TResponse` and never inspects it.
- **Positive:** No separate `IVoidPipelineBehavior` or special-casing in the mediator dispatch logic.
- **Negative:** The wrapping/unwrapping adds a small allocation per void command dispatch. Acceptable — void commands are not typically a hot path.

## Alternatives considered

- **Separate void pipeline interface:** Rejected — would double the behavior abstraction surface and require every behavior to implement two interfaces.
- **Forcing all commands to return a value:** Rejected — many commands semantically have no return value (delete, update). Forcing a dummy `Unit` type would add noise.
