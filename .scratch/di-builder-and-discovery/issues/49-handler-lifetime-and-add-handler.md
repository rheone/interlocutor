## Labels

ready-for-agent

## What to build

Handler registration via `ScanAssembly` and its siblings today always registers at a hardcoded `Scoped` lifetime, with no way to override it and no way to register a single handler manually without bypassing the builder. Make the lifetime explicit and overridable, and give handlers the same manual escape hatch behaviors already have (`AddBehavior`).

- `ScanAssembly`, `ScanAssemblies`, `ScanAssemblyContaining<TMarker>`, and `ScanEntryAssembly` each gain an added `RegistrationLifetime lifetime = RegistrationLifetime.Scoped` parameter. The default reproduces today's exact behavior - no existing caller's behavior changes.
- A new `AddHandler(Type handlerImplementationType, RegistrationLifetime lifetime = RegistrationLifetime.Scoped)` method, mirroring `AddBehavior`'s shape - including open-generic handler type definitions.
- An explicit `AddHandler` call for a given implementation type takes precedence over a duplicate of that same type discovered by a `ScanAssembly*` call in the same configuration - the explicit registration is the one actually applied to the container, not just an additional copy alongside it.
- Every accumulated registration (from scanning or from `AddHandler`) needs to record which of the two produced it. This provenance is consumed by ticket 50's registration report and by ticket 52's lifetime-conflict validation - get the shape right here rather than retrofitting it later.

## Acceptance criteria

- [ ] `ScanAssembly`/`ScanAssemblies`/`ScanAssemblyContaining<TMarker>`/`ScanEntryAssembly` each accept an optional `RegistrationLifetime`, defaulting to `Scoped`, with no behavior change for existing callers that don't pass it
- [ ] `AddHandler(Type, RegistrationLifetime)` registers a single handler, open or closed generic, at the given lifetime
- [ ] `AddHandler` for a type that a `ScanAssembly*` call in the same configuration would also discover results in the explicit registration being applied, not a duplicate
- [ ] Each accumulated `InterlocutorRegistration` carries enough provenance to distinguish "found by scanning" from "registered explicitly via `AddHandler`"
- [ ] Existing `InterlocutorBuilderTests`/`HandlerDiscoveryTests`/integration test suites continue to pass unmodified in intent
- [ ] `dotnet build` and `dotnet test` pass for the whole solution

## Blocked by

None - can start immediately. Shares `InterlocutorBuilder.cs` with ticket 40 (pipeline builder extraction) - both touch the same file from different, non-overlapping responsibility areas (this ticket stays on the discovery/defaults side ticket 40 explicitly leaves in place). Sequence rather than parallelize blindly, or diff carefully before merging both.
