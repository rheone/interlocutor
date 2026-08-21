Status: ready-for-agent

## Labels

ready-for-agent

## What to build

Make the generator's combined pipeline actually skip re-emission when nothing relevant changed, end to end - not just at one intermediate step.

Today, editing a method body inside a referenced library that changes no public API surface, or making a whitespace-only edit anywhere in the current project, still forces `InterlocutorSourceGenerator` to fully re-emit the registration module, dispatch wrappers, dispatch switch, and notification order table on every rebuild. `CompilationInfo` (ticket 12) has correct structural equality and its own pipeline step is properly cached/unchanged, but `InterlocutorSourceGenerator.Initialize`'s combined pipeline also threads the raw `Compilation` object through unconditionally - needed by `OpenGenericHandlerResolver.Resolve`'s semantic-model lookups (ticket 11) - and `Compilation` is never equal to itself across two runs. That single unconditional input defeats caching for the whole `RegisterSourceOutput` step regardless of how well-behaved every other input is.

Fix this by extracting only what open generic handler resolution actually needs from the compilation into its own equatable snapshot type (mirroring the pattern `CompilationInfo`/`ExternalModuleInfo` already established for ticket 12), so the incremental pipeline never has to combine in a live `Compilation` object just to answer "does this concrete generic usage satisfy this open handler's constraints." Once that snapshot is the only input `Execute` depends on besides the syntax-driven candidate lists, a rebuild whose snapshot compares equal to the last one can skip re-emitting source entirely.

## Acceptance criteria

- [ ] Open generic handler resolution no longer requires the raw `Compilation` object anywhere in the incremental pipeline; it consumes an equatable snapshot type instead
- [ ] A structurally-unchanged rebuild (for example, a whitespace-only or comment-only edit) reports the final `RegisterSourceOutput` step's outputs as `Cached` or `Unchanged`, not `New`, verified via `trackIncrementalGeneratorSteps: true` the way `IncrementalCachingTests` (ticket 12) already verifies the narrower `CompilationInfo` step
- [ ] Editing a referenced library in a way that changes no public API surface leaves the downstream project's generated source byte-for-byte identical, and the driver reports the corresponding step(s) as cached rather than re-emitted
- [ ] Every existing open generic handler resolution test (`OpenGenericHandlerTests`) continues to pass with identical resolution behavior - this is a caching change, not a resolution-semantics change
- [ ] Every existing cross-assembly aggregation test (`CrossAssemblyAggregationTests`) continues to pass

## Blocked by

None - can start immediately.

## Comments

Follow-up from ticket 12's code review: the review fixed `CompilationInfo`/`ExternalModuleInfo`'s broken
equality (an `ImmutableArray<T>`-wrapping record relying on compiler-generated equality, which compares the
backing array by reference) and added a regression test proving that one step is now cached correctly - but
also found and documented, rather than fixed, that the pipeline as a whole still doesn't achieve real
incrementality because of this separate, pre-existing (ticket 11-era) raw-`Compilation` dependency. See
`.scratch/di-builder-and-discovery/issues/12-cross-assembly-modules.md`'s "Incremental caching" comment for
the full trace of the finding.
