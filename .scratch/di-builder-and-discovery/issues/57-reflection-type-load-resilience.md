## Labels

ready-for-agent

## What to build

Every scan path (`HandlerDiscovery.Discover`, `InterlocutorBuilder.CandidateTypesForMatching`, and ticket 56's consumer-contract scanning) calls `Assembly.GetTypes()` directly, with no handling anywhere for `ReflectionTypeLoadException` - the exception `GetTypes()` throws when one or more types in the assembly can't be loaded (most commonly a missing transitive dependency assembly). Today that kills the entire scan with a low-signal exception naming neither the assembly nor which type(s) failed - the opposite of the "good debug info" standard the rest of this builder holds itself to (see ticket 55's `AmbiguousClosure` message, `ConflictingLifetime`, etc.).

- Catch `ReflectionTypeLoadException` at each `Assembly.GetTypes()` call site, using its `Types`/`LoaderExceptions` members to report which assembly and which specific type(s) failed to load and why.
- Continue scanning past the types that did load successfully in that assembly (`ReflectionTypeLoadException.Types` contains `null` entries for the ones that failed, alongside the successfully loaded `Type` objects for the rest) rather than aborting the whole scan.
- Surface what failed to load as a new, non-fatal entry the consumer can inspect - `RegistrationReport` (ticket 50) is the natural home, alongside `Skipped` and `Problems` - rather than only a `Console`/log write, so it stays available through `DescribeRegistrations()` and is testable.

## Acceptance criteria

- [ ] Scanning an assembly with one unloadable type still registers handlers/behaviors/closures for every other, loadable type in that assembly
- [ ] The failure is surfaced with the offending assembly's name and the loader exception detail for each type that failed, discoverable via `RegistrationReport`
- [ ] Scanning an assembly where every type loads successfully is unaffected - no behavior change, no new report entries
- [ ] `dotnet build` and `dotnet test` pass for the whole solution

## Blocked by

None - can start immediately
