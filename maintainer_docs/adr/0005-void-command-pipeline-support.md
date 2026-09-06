# ADR-0005: Void Command Pipeline Support

**Status:** Accepted (amended twice - terminology only)

## Context

Commands come in two flavors: typed commands (`ICommand<TResponse>`) that return `Outcome<TResponse>`, and void commands (`ICommand`) that return a non-generic outcome. The pipeline behavior interface is generic over `<TRequest,TResponse>`, which means it naturally works with `IPipelineBehavior<TCommand, TResponse>` for typed commands. Void commands need the same pipeline treatment - logging, validation, authorization, transactions - but produce an outcome type with no success payload instead of `Outcome<TResponse>`.

## Decision

Void commands dispatch through the same pipeline mechanism as typed commands via `VoidCommandHandlerWrapper<TCommand, TResult>`, where `TResult` is constrained to `IOutcome`. The wrapper:

1. Resolves `IVoidCommandHandler<TCommand, TResult>` from DI.
2. Uses the handler's `Task<TResult>` return directly as the pipeline's `TResponse` - no wrapping into an outer outcome, because `IPipelineBehavior<TRequest,TResponse>.Handle` already returns `Task<TResponse>` rather than a nested result.
3. Chains all registered `IPipelineBehavior<TCommand, TResult>` behaviors in reverse registration order around it.

```csharp
public override Task<TResult> Handle(object request, IServiceProvider services, CancellationToken ct)
{
    var typedRequest = (TCommand)request;
    var handler = services.GetRequiredService<IVoidCommandHandler<TCommand, TResult>>();

    Func<Task<TResult>> pipeline = () => handler.Handle(typedRequest, ct);

    foreach (var behavior in services.GetServices<IPipelineBehavior<TCommand, TResult>>().Reverse())
    {
        var next = pipeline;
        pipeline = () => behavior.Handle(typedRequest, next, ct);
    }

    return pipeline();
}
```

## Consequences

- **Positive:** Void commands get full pipeline treatment (validation, transaction, authorization, logging) with no behavior changes - every `IPipelineBehavior<,>` is already generic over `TResponse` and never inspects it.
- **Positive:** No separate `IVoidPipelineBehavior` or special-casing in the mediator dispatch logic.
- **Positive:** Because `TResponse` is the outcome type itself rather than an outcome wrapping an outcome, there is no wrap/unwrap step and no allocation beyond the closures every behavior chain already pays for.

## Alternatives considered

- **Separate void pipeline interface:** Rejected - would double the behavior abstraction surface and require every behavior to implement two interfaces.
- **Forcing all commands to return a value:** Rejected - many commands semantically have no return value (delete, update). Forcing a dummy `Unit` type would add noise.

## Amendment (terminology)

"Void command" is the name used throughout this ADR and the shipped type names (`ICommand`, `IVoidCommandHandler<,>`, `VoidCommandHandlerWrapper<,>`) - that naming is unchanged. Elsewhere in the project's documentation and glossary, the same concept is now called a **unit command**: "void" wrongly implies the handler returns nothing, when it always returns an outcome - only the success payload is empty. Read "void command" in this ADR as that same concept.

## Second Amendment (terminology, finalized to "Bare")

The amendment above renamed the concept from "void command" to "unit command" in the project's documentation and glossary, but explicitly left the shipped type names unchanged, leaving `IVoidCommandHandler<,>`/`VoidCommandHandlerWrapper<,>`/`IVoidHandlerWrapper` in code while prose said "unit command." That gap is now closed, and the name is finalized to **bare command**, not "unit": the shipped types are renamed to `IBareCommandHandler<TCommand, TResult>`, `BareCommandHandlerWrapper<TCommand, TResult>`, and `IBareHandlerWrapper`, and the project's documentation and glossary now say "bare command" rather than "unit command" - "unit" turned out to collide with the unrelated `IUnitOfWork`, a name already prominent in this same library. Tracked under `.scratch/bare-command-rename/`. Read "void command" throughout the Decision and Consequences sections above, and "unit command" in the first amendment, as this same concept under its final, shipped name.
