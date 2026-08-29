# 21 - S1694: decide whether `HandlerWrapper`'s abstract base should become an interface

**What to build:** SonarAnalyzer's S1694 ("Convert this 'abstract' class to an interface") fires twice on `src/Interlocutor.Abstractions/Core/HandlerWrapper.cs(14,25)` - it has no fields and no non-abstract members, the classic interface-shaped abstract class. Investigate whether it was deliberately made an abstract class instead of an interface for a real reason (e.g. non-public accessibility scoped to the assembly, a future extension point that needs shared state, or a generic-variance concern an interface can't express as cleanly) - if so, suppress with that reason recorded; if not, converting it to an interface is a public API shape change for a mediator-core type, so treat it as a deliberate, reviewed decision rather than a mechanical rename, and check `tests/Interlocutor.Architecture.Tests` for any assumption tied to it being a class.

**Blocked by:** None - can start immediately.

**Status:** done

- [x] A decision is made and recorded (converted to an interface, or suppressed with a genuine reason) for `HandlerWrapper`
- [x] If converted, every consumer (production and test) still compiles and behaves identically - this is a public API surface, so check for breaking-change implications before shipping
- [x] `dotnet build` reports zero `S1694` warnings
- [x] `dotnet test` passes solution-wide

## Comments

Investigated: both `HandlerWrapper<TResponse>` and its sibling `VoidHandlerWrapper<TResult>` (also flagged) are `internal`, have zero fields and only the one abstract member, and every consumer only casts a freshly `Activator.CreateInstance`-constructed instance to the type before calling `Handle` - no shared state, no extension point, no `Interlocutor.Architecture.Tests` assumption tied to being a class. Converted both to interfaces (`IHandlerWrapper<TResponse>`, `IVoidHandlerWrapper<TResult>`, renamed per this repo's `I`-prefix convention to avoid a new SA1302), updated the three implementers (`QueryHandlerWrapper`, `CommandHandlerWrapper`, `VoidCommandHandlerWrapper`) and `Mediator`'s two casts. Since both types are `internal`, this isn't a public API shape change. Recorded the safety reasoning in each interface's own `<remarks>`.
