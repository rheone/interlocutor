# 02 — Interlocutor.IntegrationTests: cross-package pipeline tracer bullet

**What to build:** A new test project, `Interlocutor.IntegrationTests`, distinct from every package's own unit-test project and distinct from `TodoTasks.Api.Tests` (which only proves the tutorial app, not the library itself). It wires a real `IServiceCollection`, calls the real `AddInterlocutor()` registration, dispatches through compile-time generated handler discovery (not reflection scanning), and commits through a real EF Core unit of work (in-memory or SQLite provider) — proving that a single command actually flows through the full canonical pipeline order (Logging → Auditing → Authorization → Validation → Transaction) end-to-end, across real package boundaries, with no mocks standing in for another Interlocutor package. This closes the gap where every existing "*IntegrationTests.cs" file today only integration-tests one package against itself.

**Blocked by:** None — can start immediately

**Status:** done - implemented in `00bbdcd`

- [x] New `tests/Interlocutor.IntegrationTests` project (its own `.csproj`, sharing this ticket's `.scratch` feature directory with ticket 01) references the real `Interlocutor.Abstractions`, `Interlocutor.DependencyInjection.Microsoft`, `Interlocutor.Generator` (as an analyzer), and `Interlocutor.Persistence.EntityFrameworkCore` packages — no fakes or mocks standing in for any Interlocutor package
- [x] At least one real command handler, registered via generated (compile-time) dispatch, exercises the full canonical behavior order and a real transactional commit
- [x] A failing case (e.g. a validation failure) proves the pipeline short-circuits before the transaction commits, observed through the real `Outcome` returned to the caller
- [x] The new project runs under `dotnet test` the same way every other `tests/*` project does
