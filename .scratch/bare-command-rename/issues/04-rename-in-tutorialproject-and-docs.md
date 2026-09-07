# 04 — Rename `IVoidCommandHandler` to `IBareCommandHandler` in `TutorialProject` and documentation

**What to build:** the sample application and every documentation page referencing the old "void"/"unit" command vocabulary are updated to the final "bare command" name, completing (not re-litigating) `maintainer_docs/adr/0005-void-command-pipeline-support.md`'s own amendment note.

**Blocked by:** 01, 02, 03 (every renamed type must exist across the whole solution before the sample app and docs can reference the final name consistently).

**Status:** done - implemented in `5d143cb`.

- [ ] `TutorialProject/TodoTasks.Api/Features/Tasks/ClearCompletedTodoTasks/ClearCompletedTodoTasksCommand.cs` and `ClearCompletedTodoTasksHandler.cs` are updated to implement `IBareCommandHandler<TCommand, TResult>`.
- [ ] `documentation/glossary.md`'s "Unit command" entry is updated to name and describe `IBareCommandHandler` as the shipped type, resolving the prose/code mismatch that motivated this rename.
- [ ] `documentation/messages-and-result-types.md`'s "ICommand (Unit Command)" heading and surrounding prose are updated to "Bare Command" and reference `IBareCommandHandler`.
- [ ] `documentation/beginners-guide-todo-api.md` and any other `documentation/` page referencing `IVoidCommandHandler` or "void command"/"unit command" prose is updated to match.
- [ ] `maintainer_docs/adr/0005-void-command-pipeline-support.md`'s "Amendment (terminology)" section gets a second amendment note recording that the shipped type names were finally updated to "Bare," pointing at this ticket set, without altering the ADR's original decision content.
- [ ] `rg -i "void command|unit command"` across the whole repository returns no hits outside of historical ADR context (`0005`'s own preserved original text) and any deliberately-preserved changelog entries.
- [ ] `dotnet build` and `dotnet test` pass for `TutorialProject` and the whole solution; `dotnet csharpier .` produces no diff.
