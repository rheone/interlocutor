# 01 - Query vs. command vs. unit-command vs. messaging decision guide

Status: done - implemented in `c66f524`. Uses "bare command" throughout, not "unit command" as this ticket's title/body name - `documentation/glossary.md`'s CQRS section retired both "void command" and "unit command" in favor of "bare command" (the latter for colliding with the unrelated `IUnitOfWork`) since this ticket was filed; `IBareCommandHandler<TCommand, TResult>` is the only such interface in `src/` today, not `IVoidCommandHandler`.

## Labels

ready-for-agent

## What to build

Add new subsections to `documentation/sample-app-walkthrough.md` that give a reader explicit "when do I reach for this" guidance across the four request/message shapes the library supports, each grounded in an example that already exists in `TutorialProject` rather than inventing new ones:

- **Query** - read-only, never mutates, never transactional. Point at `GetTodoTaskById`/`SearchTodoTasks`.
- **Value-returning command** - mutates and returns domain data the caller needs back (e.g. the created/updated resource). Point at `CreateTodoTask`/`CompleteTodoTask`.
- **Unit command** - mutates but has no domain payload to return, only an outcome (`ICommand` + `IVoidCommandHandler<TCommand, TResult>`). Use the term "unit command" throughout, not "void command" - `documentation/glossary.md` already retires "void" as a misnomer for this project's prose. Point at `ClearCompletedTodoTasks`.
- **Messaging** - fire-and-forget fan-out to independent subscribers that don't participate in the triggering command's response (`INotification`/`IPublisher`). Point at the existing `CompleteTodoTask` -> `TodoTaskCompletedNotification` example (two subscribers), per `documentation/sample-app-walkthrough.md`'s existing "Multi-subscriber notification" section and `documentation/notifications-and-fan-out.md`.

Frame it as a decision guide (a short table or a "use X when Y" passage per shape), not a restatement of what each shape is - the definitions already exist in `documentation/glossary.md` and `README.md`; this ticket adds the missing "when/why," not new definitions. No new code - every example cited already exists and already passes its own tests.

## Acceptance criteria

- [x] A reader can tell, from this section alone, which of the four shapes to pick for a new feature and why
- [x] Every example cited already exists in `TutorialProject` today - no new handlers introduced by this ticket
- [x] Uses "bare command" consistently (see status note above for why, not "unit"/"void command")
- [x] No enumerated counts in prose beyond the closed-set exception (house style)
- [x] No citations to `maintainer_docs/` paths to justify the guidance (house style) - state the rationale as fact
- [x] Glossary terms (`bare command`, `INotification`) linked on first mention only; `Query`/`Command` skipped since the file already explains both inline earlier

## Blocked by

None - can start immediately.
