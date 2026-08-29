Status: backlog - deferred, do not start until explicitly promoted to ready-for-agent

## Labels

needs-triage

## What to build

`documentation/beginners-guide-todo-api.md` Step 6's "Bonus: paging" section - `PagedTodoTasksDto`, `ListTodoTasksPagedQuery`/handler using `IDomainRepository<TEntity,TKey>.GetPagedAsync` and its `Page<T>` return shape (from `Interlocutor.Persistence.Abstractions`), mapped into the project's own `PagedTodoTasksDto` success type. Advanced-feature backlog item - demonstrates bounding an unbounded list once `ListTodoTasksQuery` (ticket 06) stops being a safe default.

## Acceptance criteria

- [ ] `PagedTodoTasksDto`/`ListTodoTasksPagedQuery`/handler/endpoint exist, as written in the guide, with unit and HTTP integration test coverage (including a page boundary/last-page-partial-count case)
- [ ] A code comment explains why `Page<T>` itself isn't returned directly as the outcome's success case (it's repository-return-shape vocabulary, not `OperationOutcome` success-case vocabulary)

## Blocked by

12
