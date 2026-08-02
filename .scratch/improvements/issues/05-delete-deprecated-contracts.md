## Labels

ready-for-agent

## What to build

Several request contracts and one result type were commented out mid-migration and never restored. They are deprecated: the standard command, query and handler contracts already cover these cases through their generic type parameters, so a parallel set of bulk, enumerable and paged contracts is redundant surface.

Delete them outright rather than leaving commented-out bodies, and remove the build-noise directives left behind. The bulk item validation behavior goes with them, since its only reason to exist was the bulk command contract.

Because all of these are currently documented, the documentation must be updated in the same slice, including a short note explaining that the removal was deliberate so a future reader does not re-add them.

## Acceptance criteria

- [ ] The bulk, enumerable and paged request contracts are deleted, along with the bulk item validation behavior and the paged result type
- [ ] No commented-out bodies or build-noise directives remain in the source tree
- [ ] No references to the removed types survive in source, tests, or documentation
- [ ] Package metadata no longer advertises the removed result type
- [ ] Documentation records that the removal was deliberate and names the generic-based replacement approach

## Blocked by

None — can start immediately
