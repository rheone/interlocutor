> **Status: done** - merged in `143a9b1`; see resolved note below.

## Labels

ready-for-agent

## What to build

`OpenGenericHandlerResolver.Resolve` closes an open generic handler over a request type only when both the
open handler and a fully closed usage of that instantiation are discovered by the *same* generator run - both
`closedHandlers`/`openHandlers` (from `context.SyntaxProvider` over handler-shaped syntax) and `usages` (from
`GenericUsageAnalyzer`, also `context.SyntaxProvider`) are scoped to the compiling project's own syntax trees.
An open generic handler defined in a referenced library is invisible to an application project's closing pass
- the application's generator run never sees the library's syntax, only its metadata - and a closed usage
written in the application is equally invisible to the library's own generator run, which already finished
before the application exists. Neither side ever closes it, so the handler silently never resolves:

```
System.InvalidOperationException: No service for type 'Interlocutor.Abstractions.Queries.IQueryHandler`2[...]' has been registered.
```

Reproduced directly building `samples/Interlocutor.Sample.WebApi` (ticket 18): `GetByIdHandler<TEntity>` (an
open generic handler) lives in `Interlocutor.Sample.Handlers`; the web application dispatched
`GetByIdQuery<TodoItem>`/`GetByIdQuery<TagItem>` directly from its own `Program.cs`, and both closings were
silently missing from the aggregated registration module (no build error, no diagnostic - `AddInterlocutor()`
succeeded; the failure only surfaced at dispatch time). Worked around by moving the closing usages into the
handler library itself (`Interlocutor.Sample.Handlers.Shared.GetByIdQueries`, typed entry points beside
`GetByIdHandler<TEntity>`), which the web application calls instead of constructing `GetByIdQuery<TEntity>`
directly - see that type's remarks.

This is a real gap in the ticket 11 (open-generic-handlers) / ticket 12 (cross-assembly-modules) combination:
ticket 12 solved cross-assembly aggregation for ordinary closed handlers, but open generic handler *closing*
was never extended to look at usages or handler candidates from referenced assemblies' generated module info
the way `ReferencedModuleDiscovery` already does for closed handlers.

## Acceptance criteria

- [x] An open generic handler defined in library assembly A, used only via a closed instantiation in
      application assembly B (which references A), closes correctly and resolves at dispatch time - either by
      extending closing to consider referenced assemblies' open-handler and usage information, or by emitting a
      diagnostic when a closed generic request usage has no available handler (closed or open) anywhere in the
      reference graph, so this fails at build time instead of at first dispatch — **Status: resolved via the
      diagnostic path this criterion itself allows for.** Extending closing to span assemblies (option one)
      would require serializing `OpenGenericHandlerModel`'s full constraint set - `GenericParameterConstraintModel`
      entries, checked today via `GenericConstraintSnapshot` built from live `Compilation` symbols - across
      assembly-level attributes, faithfully enough to re-run `GenericConstraintChecker` in a downstream
      compilation that never sees library A's syntax. That's a materially larger, higher-risk undertaking than
      this ticket's scope; the diagnostic path is the one explicitly sanctioned as sufficient by this same
      bullet and by the ticket's last bullet below. `IQuery<TResponse>`/`ICommand<TResponse>` usage
      classification in `GenericUsageAnalyzer` resolves across the whole reference graph regardless (Roslyn's
      semantic model does this for free), so the diagnostic itself is accurate cross-assembly even though
      closing is not.
- [x] A regression test proves the cross-assembly gap is now caught at build time - **adjusted from "closes
      correctly and resolves at dispatch time" to match the diagnostic path actually taken**:
      `CrossAssemblyOpenGenericHandlerTests.ClosedUsageInApplication_OfOpenHandlerDeclaredOnlyInReferencedLibrary_ReportsUnresolvedGenericRequestUsage`
      emits a real library assembly (`GeneratorTestHarness.RunAndUpdate`/`ToMetadataReference`, the same
      pattern `CrossAssemblyContainerResolutionTests` and `CrossAssemblyAggregationTests` use) declaring an
      open generic handler with no local usage, references it from a separate application compilation with a
      closed usage only, and asserts `INTERLOC008` (`UnresolvedGenericRequestUsage`) is reported naming the
      unresolved usage - the same silent gap that previously only surfaced as
      `HandlerNotRegisteredException` at first dispatch (reproduced directly in
      `samples/Interlocutor.Sample.WebApi` before that sample worked around it, per this ticket's own
      description) now fails the build instead.
- [x] The same-compilation-only case (today's only working case) has no regression -
      `CrossAssemblyOpenGenericHandlerTests.ClosedUsageInSameCompilationAsOpenHandler_ReportsNoUnresolvedGenericRequestUsageDiagnostic`
      asserts INTERLOC008 does not fire once a usage resolves locally, and the full pre-existing
      `OpenGenericHandlerTests` suite (closed-beats-open precedence, constraint checking, `HandlerOverrideAttribute`
      ambiguity) still passes unchanged. A second, previously-untested regression is now covered too:
      `OpenGenericHandlerTests.AmbiguousUsage_ReportsOnlyOpenGenericHandlerAmbiguity_NotAlsoUnresolvedGenericRequestUsage`
      proves a usage left ambiguous between two local open generic handlers (INTERLOC006) is never *also*
      reported as INTERLOC008 - the `AttemptedUsageDisplayNames` exclusion this ticket's resolution relies on
      was previously only a code comment's claim, not an asserted test.
- [x] Closing does not span assemblies, for the structural reason above; documented on
      `OpenGenericHandlerResolver`'s own remarks (referencing the new diagnostic) and on
      `InterlocutorDiagnostics.UnresolvedGenericRequestUsage`'s XML docs, which replace the previous silent,
      first-dispatch-time failure with `INTERLOC008` at build time, escalatable to an error via
      `.editorconfig` the same way `MissingHandler` (`INTERLOC001`) already is.

## Blocked by

None - can start immediately
