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

- [ ] An open generic handler defined in library assembly A, used only via a closed instantiation in
      application assembly B (which references A), closes correctly and resolves at dispatch time - either by
      extending closing to consider referenced assemblies' open-handler and usage information, or by emitting a
      diagnostic when a closed generic request usage has no available handler (closed or open) anywhere in the
      reference graph, so this fails at build time instead of at first dispatch
- [ ] A regression test proves the cross-assembly open-generic-handler-closing case end to end (real emitted
      assemblies, real container, real dispatch - following the pattern `CrossAssemblyContainerResolutionTests`,
      ticket 54, already established for ordinary closed handlers)
- [ ] The same-compilation-only case (today's only working case) has no regression
- [ ] If closing genuinely cannot span assemblies for a structural reason, that constraint is documented on
      `OpenGenericHandlerResolver`/the open-generic-handlers documentation, and a build-time diagnostic replaces
      the current silent, first-dispatch-time failure

## Blocked by

None - can start immediately
