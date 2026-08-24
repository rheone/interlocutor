> **Status: done** - implemented on the `improvements` branch (commit `e75e815`): `CrossAssemblyContainerResolutionTests` exercises real `Microsoft.Extensions.DependencyInjection` and Autofac containers against a handler declared only in a referenced library assembly. `GeneratorTestHarness` gained `EmitAndLoadAssembly` - compiles and emits a real in-memory PE via `Compilation.Emit`, loads it via `Assembly.Load`, and repeats for an app assembly referencing the library's real PE - simulating a genuine project reference rather than an in-memory `Compilation` reference. Ticket 12's "Container coverage gap" comment is closed out.

Status: ready-for-agent

## Labels

ready-for-agent

## What to build

Prove, with a real end-to-end test rather than an in-memory `Compilation` comparison, that a handler declared only in a referenced library resolves and dispatches correctly through both containers once the application aggregates that library's generated module.

Ticket 12's `CrossAssemblyAggregationTests` proves aggregation is correct at the generated-source-text level: two in-memory `Compilation`s (never emitted to real assemblies, never loaded), asserting the emitted source references the right types. `GeneratedRegistrationIntegrationTests` in both `Interlocutor.DependencyInjection.Microsoft.Tests` and `.Autofac.Tests` exercise a real container, but only against a single project's own generated module - they were updated for ticket 12's namespace rename, not extended to a genuinely aggregated one. Nothing today emits two real assemblies, loads the downstream one, and resolves a handler from its aggregated module through a real container.

Build a helper that compiles a "library" source to a real in-memory PE (`Compilation.Emit` to a `MemoryStream`, not just `ToMetadataReference`), loads it via `Assembly.Load`, then compiles an "app" source referencing the library's PE as a metadata reference, runs the generator over the app, emits and loads the app assembly too. Read the app's aggregated `Registrations` property via reflection, feed it into a real `ServiceCollection`/`ContainerBuilder`, and dispatch a request whose handler lives only in the library assembly.

## Acceptance criteria

- [ ] A test helper emits two real, loaded assemblies (library + app) with the generator run over each, simulating a genuine project reference rather than an in-memory `Compilation` reference
- [ ] A `Microsoft.Extensions.DependencyInjection` container built from the app's aggregated `Registrations` resolves and dispatches a request to a handler declared only in the library assembly
- [ ] An Autofac container does the same, from the same aggregated `Registrations`
- [ ] Ticket 12's "Container coverage gap" comment is closed out and its "Both containers work against aggregated registrations" acceptance criterion is re-checked, referencing this ticket

## Blocked by

None - can start immediately.

## Comments

Follow-up from ticket 12's code review (Spec axis): the aggregated-registrations acceptance criterion was
checked off on the strength of tests that only prove single-project resolution plus a namespace rename, not
genuine cross-assembly resolution. See
`.scratch/di-builder-and-discovery/issues/12-cross-assembly-modules.md`'s "Container coverage gap" comment for
the full finding.
