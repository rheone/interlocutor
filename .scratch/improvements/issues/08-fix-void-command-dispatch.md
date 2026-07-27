## Labels

ready-for-agent

## What to build

Rewrite `Mediator.Send(ICommand)` (the non-generic, void overload) in `Mediance.Abstractions` to use the same cached generic wrapper pattern as `ICommand<TResponse>`, instead of the current `GetMethod` + `Invoke` reflection.

Current implementation at `Mediator.cs` around line 80—90:
```csharp
public async Task<Result> Send(ICommand command, CancellationToken ct = default)
{
    // Uses GetMethod + Invoke — no pipeline behaviors run
}
```

The fix:
1. Create a `VoidCommandHandlerWrapper` analogous to the existing typed wrapper
2. It resolves `ICommandHandler<>` from DI, wraps it with pipeline behaviors, and returns `Result`
3. Cache it in the same `ConcurrentDictionary` by request type
4. Add tests verifying pipeline behaviors (e.g., a stub `ValidationBehavior`) execute for void commands

Pipeline behaviors currently only apply in the typed wrappers. This means any behavior registered for `ICommand` does nothing — validation, logging, transaction handling, etc. all silently skip void commands.

## Acceptance criteria

- [ ] `Send(ICommand)` uses a cached `VoidCommandHandlerWrapper` (not raw `MethodInfo.Invoke`)
- [ ] Pipeline behaviors run for void commands (test with a stub behavior that tracks invocation)
- [ ] Handler is correctly resolved from DI per request type
- [ ] All existing tests still pass

## Blocked by

None — can start immediately
