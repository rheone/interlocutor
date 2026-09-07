# 03 - Implement ResourceCommandHandler's decided shape everywhere it's named

**What to build:** Ship whichever shape issue 02 recorded, end to end - not just the type declaration. Whether that's the full `Eject.WithCase` redesign (`ResourceCommandHandler<TCommand, TEntity, TResponse>`, generic over `IUnion`) or the fallback plain rename (`TSuccess` -> `TResponse`, shape unchanged), every live reference to the old `TSuccess`-named, fixed-shape signature needs to match the new one:

- `src/Interlocutor.Abstractions/Commands/ResourceCommandHandler.cs` itself and its XML doc comments
- `src/Interlocutor.Abstractions/PublicAPI.Unshipped.txt`
- `documentation/handler-selection-and-dispatch.md`
- `documentation/sample-app-walkthrough.md`
- `documentation/beginners-guide-todo-api.md`
- `documentation/api-reference/interlocutor-abstractions.md`
- `src/Interlocutor.Generator/Diagnostics/InterlocutorDiagnostics.cs`'s `INTERLOC010` remarks
- Every `TutorialProject` doc-comment mention of `ResourceCommandHandler{TCommand,TEntity,TSuccess}` (`CompleteTodoTaskCommand.cs`, `CompleteTodoTaskHandler.cs`, `UpdateTodoTaskCommand.cs`, `UpdateTodoTaskHandler.cs`)

Leave `AuthorizationBehavior.cs`'s own remarks alone - its `TSuccess` mentions are deliberately historical prose contrasting its own retired shape, unrelated to this rename.

If issue 02 chose the full redesign, this ticket also migrates every `TutorialProject` command still built on `ResourceCommandHandler` off direct case construction and onto whatever the spike's worked example established, and updates any test doubles that assert on the old fixed-shape behavior.

**Blocked by:** 02

**Status:** ready-for-agent

- [ ] `ResourceCommandHandler`'s declaration and every doc/prose call site listed above matches the shape issue 02 recorded - `rg -n "TSuccess"` in `src/`, `documentation/`, and `TutorialProject/` returns only `AuthorizationBehavior.cs`'s deliberate historical mentions
- [ ] If the full redesign was chosen: `NotFound`/`Forbidden` are ejected via `Eject.WithCase` rather than constructed directly, matching issue 01's proven mechanism
- [ ] `dotnet build` and `dotnet test` pass for the whole solution; `dotnet csharpier .` produces no diff
- [ ] `TutorialProject` still compiles and its existing tests pass unchanged in observed behavior

## Comments
