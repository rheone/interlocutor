# 12 - TodoTasks feature: Paging

**What to build:** A paged variant of the TodoTasks list endpoint, using this repo's own `Page<T>`/paging conventions (see `documentation/persistence-abstraction.md`).

**Blocked by:** 06

**Status:** ready-for-agent

- [ ] The list endpoint accepts paging parameters and returns a `Page<T>`-shaped result with correct total counts
- [ ] Persistence and HTTP tests cover at least a partial page, an empty page, and a boundary page
