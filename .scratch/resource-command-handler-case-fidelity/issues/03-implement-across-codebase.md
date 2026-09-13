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

**Status:** done - implemented in `<PENDING_SHA>`

- [x] `ResourceCommandHandler`'s declaration and every doc/prose call site listed above matches the shape issue 02 recorded - `rg -n "TSuccess"` in `src/`, `documentation/`, and `TutorialProject/` returns only `AuthorizationBehavior.cs`'s deliberate historical mentions
- [x] If the full redesign was chosen: `NotFound`/`Forbidden` are ejected via `Eject.WithCase` rather than constructed directly, matching issue 01's proven mechanism
- [x] `dotnet build` and `dotnet test` pass for the whole solution; `dotnet csharpier .` produces no diff
- [x] `TutorialProject` still compiles and its existing tests pass unchanged in observed behavior

## Comments

This session resumed after a prior session was killed by a restart; its uncommitted edits (`ResourceCommandHandler.cs` itself, `PublicAPI.Unshipped.txt`, all four listed docs, `InterlocutorDiagnostics.cs`'s `INTERLOC010` remarks, `TutorialProject`'s `CompleteTodoTaskCommand.cs`/`CompleteTodoTaskHandler.cs` migration onto the template method, and the matching test updates) were verified correct and complete, then built on rather than redone. `UpdateTodoTaskCommand.cs`/`UpdateTodoTaskHandler.cs` needed no changes - they were already updated to `TResponse` in the redesign's own spike commit (`be5adbb`).

Remaining `TSuccess` hits resolved for the strict `rg` acceptance check, beyond the ticket's literal file list:

- `src/Interlocutor.AspNetCore/ResultExtensions.cs` - `ClassifySuccessCaseForActionResult`/`ClassifySuccessCaseForMinimalApiResult`'s generic parameter renamed `TSuccess` -> `TValue` (unrelated to `ResourceCommandHandler`; matches the existing `value` parameter name rather than reusing the sibling `ClassifyFailureCaseFor*<TFailure>` helpers' name, since `TFailure` genuinely binds to a known declared-failure case type while this one is an unconstrained "whatever occupies the slot" value).
- `src/Interlocutor.Abstractions/Pipeline/UnionCaseFactory.cs` - doc-comment example rewritten to `Outcome<_, Forbidden>` (an underscore placeholder, not `TResponse`) after an initial `TResponse` rename proved self-contradictory: this doc comment already uses `TResponse` to mean the *whole* union response type a few lines away (`FindCaseConstructor<TResponse, TCase>`), so reusing it for a hypothetical *decomposed* single-case slot would say something false.
- `src/Interlocutor.Abstractions/Commands/ResourceCommandHandler.cs:27` and `TutorialProject/.../CompleteTodoTaskHandler.cs`'s own historical remarks (contrasting each type's current shape against its own retired `TSuccess`-shaped past) were rewritten as `Outcome<_, NotFound, Forbidden>` rather than treated as a second `AuthorizationBehavior.cs`-style carve-out: both files are explicitly named in this ticket's own file list (`AuthorizationBehavior.cs` is not), so leaving a `TSuccess` mention in either would be inconsistent with that instruction and would fail the acceptance check's literal `rg` scope.
- `AuthorizationBehavior.cs`'s four `TSuccess` mentions were left untouched, per the ticket's explicit carve-out.

Verified via `dotnet build` (solution-wide, 0 errors) and `dotnet test` (1432 total, 1356 passed, 76 failed - all 76 in `TodoTasks.Api.Tests`, all the same pre-existing `WebApplicationFactory`/`Microsoft.Extensions.Primitives.ChangeToken.OnChange` `MissingMethodException` tied to the out-of-scope `Directory.Packages.props`/`dotnet-tools.json` package-version changes already on this branch before this ticket; every other test project, including the `CompleteTodoTaskHandler` unit tests that don't boot a host, passed clean). `dotnet csharpier check` confirmed zero diff on every file this ticket touched. Code review (Standards + Spec axes) found two genuine issues (a leftover `TSuccess` in newly-added `CompleteTodoTaskHandler.cs` prose, and the `UnionCaseFactory.cs` `TResponse` self-contradiction above) - both fixed and re-verified.
