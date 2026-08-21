> **Status: done** - implemented on branch `worktree-agent-a03c1c713329c98d9` (commits `77b7fc0`, `f877dff`, `70b040d`, `1c5348c`).

## Labels

ready-for-agent

## What to build

Make a multi-project solution work with no extra configuration.

A developer puts their handlers in a class library and references it from a web application. Handlers are discovered and registered with a single configuration call in the application, and neither project lists the other's assemblies anywhere.

Each handler-defining assembly runs the generator and emits its own registration module identified by an assembly-level attribute. The application-level generator discovers those modules by attribute across referenced assemblies and aggregates them. This keeps rebuilds incremental - changing one library regenerates only that library's module - and lets a class library ship pre-generated registrations to consumers.

The alternative of having only the entry project walk every referenced assembly's metadata was considered and rejected: it re-scans the whole reference graph on every build and cannot see handlers in assemblies it does not directly reference.

Handlers in assemblies loaded at runtime rather than referenced at compile time remain the province of the explicitly opted-in runtime scanning fallback.

## Acceptance criteria

- [x] Handlers in a referenced class library are discovered with no assembly list and no extra configuration
- [x] Each handler-defining assembly emits its own registration module identified by an assembly-level attribute
- [x] The application-level generator aggregates modules found across referenced assemblies
- [x] Three-level referencing works: application references library A which references library B
- [x] Changing one library does not force regeneration of the others
- [x] A missing-handler diagnostic still fires for a request declared in one assembly whose handler is absent everywhere
- [x] Duplicate registration does not occur when an assembly is referenced through more than one path
- [x] Both containers work against aggregated registrations

## Blocked by

10 - Generator foundation (done)

## Comments

Implemented purely at compile time, via Roslyn - never `System.Reflection` at run time (the original
`InterlocutorGeneratedModuleAttribute` remarks described a runtime-reflection design; that was superseded and
the remarks were rewritten in commit `77b7fc0`).

**Discovery.** `ReferencedModuleDiscovery.Discover` walks `Compilation.References`, resolves each reference's
`IAssemblySymbol`, and reads `InterlocutorGeneratedModuleAttribute`/`InterlocutorGeneratedHandlerAttribute`
straight out of assembly-level metadata via `IAssemblySymbol.GetAttributes()` - the same mechanism compilers
use for `InternalsVisibleTo`. `Compilation.References` already flattens a project reference chain (MSBuild
resolves a library's own project references into direct assembly references for every downstream consumer),
so three-level referencing (app → library A → library B) needs no manual transitive walk: the app's own
`Compilation.References` already contains B directly. Deduplication is by `IAssemblySymbol.Identity`, so a
diamond-shaped reference graph (B reachable both directly and via A) contributes exactly one aggregation call
site.

**Naming had to change.** The generated module's namespace moved from the bare `Interlocutor.Generated` to
`Interlocutor.Generated.<SanitizedAssemblyName>`. This is not cosmetic - it was required before any
aggregation could work at all: the moment an application both runs the generator over its own code and
references a library that also ran it, an unqualified module name collides (CS0433, a type existing in both
the current compilation and a referenced assembly) between the app's own source-declared module and the
library's referenced one. Both `Interlocutor.DependencyInjection.Microsoft.Tests` and
`.Autofac.Tests`' `GeneratedRegistrationIntegrationTests` were updated for the new spelling
(`Interlocutor.Generated.Interlocutor_DependencyInjection_Microsoft_Tests.InterlocutorGeneratedRegistrations`,
etc.) - any other hand-written reference to the old fixed name needs the same update.

**Missing-handler diagnostic.** `InterlocutorGeneratedHandlerAttribute` (new, in
`Interlocutor.DependencyInjection.Abstractions`) restates each query/command/void-command handler's
(request type, response type, kind) as its own assembly attribute, separately from the module's
`Registrations` property - `Registrations` is a compiled static-property initializer Roslyn's symbol APIs
cannot read back out of another assembly's IL without evaluating it, so the diagnostic check needed its own,
directly-inspectable metadata. Matching is by fully-qualified name string, same as the existing
same-compilation check, not by symbol identity across compilations.

**Containers.** Neither `Interlocutor.DependencyInjection.Microsoft` nor `.Autofac` needed any change:
`InterlocutorBuilder.AddGeneratedRegistrations` already accepts any `IEnumerable<InterlocutorRegistration>`,
and the aggregated module exposes the exact same shape a single-project module always did - confirmed by
running both containers' existing `GeneratedRegistrationIntegrationTests` unchanged apart from the module
name update above.

**Interpretation note for reviewers.** The ticket's design-questions text ("the missing-handler diagnostic
must still fire correctly when a request is declared in one assembly and its handler lives in another") reads
as if a single project could see both halves of such a pair and have the diagnostic flip from firing to not
firing. Under acyclic project references this specific shape cannot arise: a compilation only produces a
`RequestModel` for a type it syntactically *declares* itself, never for a type it merely references from
another assembly - so a project that references both the request-declaring assembly and the
handler-implementing assembly asks no question about that request at all (nothing to suppress), and the
request-declaring assembly itself can never reference forward to whatever downstream library eventually
implements the handler (that would be a circular reference). `CrossAssemblyAggregationTests` instead verifies
the two properties that are both testable and load-bearing: the diagnostic still fires when a locally-declared
request truly has no handler anywhere in the reference graph (guards against over-eager suppression), and it
does not misfire when referencing a library whose own request/handler pairs are already fully matched
(guards against stale/duplicated diagnostics leaking across the aggregation boundary).

**Known gap, not introduced by this ticket.**
`Interlocutor.DependencyInjection.Microsoft.Tests.GeneratedRegistrationIntegrationTests.GeneratedRegistration_WithMultipleBehaviors_ExecutesBehaviorsInCorrectOrder`
fails on `main`/`improvements` before this ticket's changes too (confirmed by stashing this ticket's work and
re-running it) - `AddBehavior(typeof(OrderTrackingBehaviorN))` registers a *closed* type against the *open*
`IPipelineBehavior<,>` service type, which Microsoft.Extensions.DependencyInjection rejects
(`ArgumentException: Open generic service type ... requires registering an open generic implementation
type.`). Left unfixed as out of scope for this ticket.
