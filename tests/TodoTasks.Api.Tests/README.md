# TodoTasks.Api.Tests

Three test layers, one project - each folder below is a layer, not a feature grouping. Every
feature ticket (see `.scratch/tutorial-project/`) adds tests to whichever of these layers actually
exercises what it built; most features touch all three.

## `Unit/`

Handler and validator logic in isolation, no database, no HTTP. `IDomainRepository<TEntity,TKey>`
and `IUnitOfWork` are NSubstitute-mocked - the same pattern this repo's own `tests/*.Tests` projects
use for testing Interlocutor's own behaviors (see e.g.
`tests/Interlocutor.Persistence.NHibernate.Tests`). Fast, no setup - this is where most of a
feature's edge cases (validation failures, `Conflict`/`NotFound` cases, ownership checks) should
actually live, since they don't need a real database to prove.

## `Persistence/`

Proves the Fluent NHibernate mappings themselves are correct - that an entity actually round-trips
through a real in-memory SQLite database, not just that a handler calls the right repository method.
`PersistenceFixture` (see `Persistence/PersistenceFixture.cs`) opens the same kind of single
kept-alive connection `TodoTasksSessionFactory` builds for the running app, via that exact same
method - not a hand-rolled copy of its logic - so these tests exercise the real mapping/schema code
path, not a test-only stand-in for it.

## `Http/`

End-to-end tests through real HTTP requests, via `TodoTasksApiFactory`
(`Http/TodoTasksApiFactory.cs`, a thin `WebApplicationFactory<Program>`). Each factory instance
boots the real `Program.cs` - Autofac container, NHibernate schema export, the full pipeline - so
each test class gets its own fresh in-memory database for free, with no extra setup. This is the
layer that proves a feature's DI wiring, authentication (once ticket 04 lands), and HTTP status
mapping all actually work together, not just that the handler logic is correct in isolation.
