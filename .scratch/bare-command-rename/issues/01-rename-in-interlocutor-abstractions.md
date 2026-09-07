# 01 — Rename `IVoidCommandHandler` to `IBareCommandHandler` in `Interlocutor.Abstractions`

**What to build:** `IVoidCommandHandler<TCommand, TResult>`, `VoidCommandHandlerWrapper<TCommand, TResult>`, and `IVoidHandlerWrapper` become `IBareCommandHandler<TCommand, TResult>`, `BareCommandHandlerWrapper<TCommand, TResult>`, and `IBareHandlerWrapper`, along with every in-package reference to them. Pure identifier rename, no behavioral change. The non-generic `ICommand` request-side marker is not renamed - it was never called "Void" in its own type name.

**Blocked by:** None — can start immediately.

**Status:** done - implemented in `e700e26`.

- [ ] `IVoidCommandHandler<TCommand, TResult>` → `IBareCommandHandler<TCommand, TResult>`; `VoidCommandHandlerWrapper<TCommand, TResult>` → `BareCommandHandlerWrapper<TCommand, TResult>`; `IVoidHandlerWrapper` → `IBareHandlerWrapper`, each in its own correctly-renamed file.
- [ ] Every in-package reference is updated: `Mediator`, `ISender`, `PipelineChainExecutor`, `PipelineChainResolver`, `PipelineEjectException`, `QueryHandlerWrapper`, `ReflectionRequestDispatcher`, `CommandHandlerWrapper`, and `ICommandHandler`'s doc-comment cross-reference.
- [ ] XML doc comments on the renamed types/members updated to say "bare command" wherever they previously said "void command"/"void commands."
- [ ] Every test in `tests/Interlocutor.Abstractions.Tests` (`MediatorActivityTracingTests`, `MediatorTests`, `RuntimeTypedDispatchTests`) updated to the new names and continues to pass.
- [ ] `PublicAPI.Unshipped.txt` (`Interlocutor.Abstractions`) updated for every renamed public type/member.
- [ ] `rg -i "void.?command"` inside `Interlocutor.Abstractions`'s own `src/`/`tests/` returns no hits other than intentional historical references (if any remain, e.g. in a changelog).
- [ ] `dotnet build` and `dotnet test` pass for `Interlocutor.Abstractions`; `dotnet csharpier .` produces no diff.
