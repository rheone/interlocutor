Status: ready-for-agent

## Labels

ready-for-agent

## What to build

`documentation/beginners-guide-todo-api.md` Step 6's "Bonus: querying directly against the session" section - `SearchTodoTasksQuery`/`SearchTodoTasksHandler` using NHibernate's QueryOver API directly against an injected `Lazy<ISession>`, bypassing `IDomainRepository<TEntity,TKey>`, for an insensitive-like title search. Advanced-feature backlog item - demonstrates dropping to the raw session when the repository's fixed method set isn't expressive enough, while still sharing the same scoped session/transaction as everything else in the dispatch.

## Acceptance criteria

- [ ] `SearchTodoTasksQuery`/handler/endpoint exist, as written in the guide, with unit and HTTP integration test coverage
- [ ] A code comment explains why this bypasses the repository and why that's still safe (same scoped `ISession`, NHibernate's one-session-per-thread/transaction concurrency contract)

## Blocked by

12 (done - this ticket can start immediately)

## Comments

Promoted to `ready-for-agent` via `/to-tickets`, alongside 15-18 - independent of each other, each already a single self-contained vertical slice.
