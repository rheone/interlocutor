# 06 - TodoTasks feature: Read (GetById + List)

**What to build:** GetById and List endpoints/queries/handlers for TodoTasks, owner-scoped to the authenticated caller.

**Blocked by:** 05

**Status:** ready-for-agent

- [ ] `GET` by id returns the task if owned by the caller, not-found otherwise (including when it exists but is owned by someone else)
- [ ] `GET` list returns only the caller's own tasks
- [ ] Unit, persistence, and HTTP tests cover both endpoints
