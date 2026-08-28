> **Status: done** - implemented alongside the other pipeline-overrides tickets in ADR-0019.

## Labels

ready-for-agent

## What to build

The override capability from ticket 01 behaves identically when the application is wired with Autofac instead of Microsoft DI - same declaration API, same resolved pipeline, same exclusivity guarantee against marker-constrained behaviors. Prove it with the same kind of mirrored test coverage the codebase already carries for its other container-neutral behavior-composition features.

Update the relevant Autofac-facing documentation to confirm parity, following the pattern already used there for other features this library supports identically across both containers.

## Acceptance criteria

- [ ] Declaring an override pipeline and dispatching a matching request produces the identical resolved pipeline under Autofac as under Microsoft DI
- [ ] Existing Autofac-registered applications that don't declare an override see no behavior change
- [ ] `documentation/dependency-injection-autofac.md` confirms container parity for this feature

## Blocked by

01 - Core override mechanism (Microsoft DI, typed dispatch path)
