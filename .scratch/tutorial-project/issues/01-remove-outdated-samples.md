Status: open

## Labels

ready-for-agent

## What to build

Delete `samples/Interlocutor.Sample.Handlers/` and `samples/Interlocutor.Sample.WebApi/` entirely (both projects, including `bin`/`obj` if present), and remove their two `<Project Path=...>` entries plus the now-empty `/samples/` solution folder from `Interlocutor.slnx`.

Confirm first that nothing outside `samples/` and `documentation/sample-app-walkthrough.md` references these two projects (a project reference, a `dotnet run` instruction elsewhere, a CI step) before deleting - if something unexpected does, stop and flag it rather than silently deleting through it.

This is a pure removal ticket - it deliberately leaves `documentation/sample-app-walkthrough.md` describing a now-deleted project. That's expected and resolved by ticket 12 once the replacement project's shape is known; don't rewrite that doc here.

## Acceptance criteria

- [ ] `samples/` directory no longer exists
- [ ] `Interlocutor.slnx` has no `/samples/` folder and no reference to either deleted project
- [ ] `dotnet build` succeeds for the remaining solution
- [ ] `dotnet test` passes for the remaining solution (no test project referenced either deleted sample)

## Blocked by

None - can start immediately
