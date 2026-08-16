## Labels

ready-for-agent

## What to build

Move the library's central convention from documentation into the type system.

The README states that handlers return outcomes so the compiler forces callers to handle every case. But the query and command request interfaces place no constraint on their response type — only the unit-command dispatch overload does. A developer can return a bare DTO and lose the entire guarantee with nothing to stop them.

Constrain the response type parameter on the query and command request interfaces to the base outcome interface, matching the constraint the unit-command overload already carries. This also makes the runtime-typed dispatch of ticket 09 sound, since that overload can only promise an inspectable outcome if every response is one.

Also delete the commented-out enumerable-query contract, which has sat as a dead file with no live lines.

**This is a wide change with no expand step available** — an interface constraint has no "beside the old one" form, so it lands atomically and every implementation must satisfy it in the same commit. Expect the blast radius to be the test projects. It is breaking for any consumer returning a bare DTO, which is pre-1.0 acceptable and is behavior the README already advises against.

Note for scheduling: this ticket has no dependency on the builder work, but it edits many of the same test files as ticket 04. Running the two concurrently means merge pain even though neither gates the other.

## Acceptance criteria

- [ ] Query and command request interfaces constrain their response type to the base outcome interface
- [ ] The commented-out enumerable-query file is deleted
- [ ] Every request and handler in the repository satisfies the constraint
- [ ] The full test suite passes
- [ ] The glossary reflects the constraint

## Blocked by

None — can start immediately
