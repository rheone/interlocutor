# ADR-0008: Compile-Time Handler Discovery via Source Generator

**Status:** Accepted

Supersedes [ADR-0003](0003-mediator-as-service-locator.md).

## Context

Handler registration is currently runtime assembly scanning: `AddInterlocutor()` reflects over one or more assemblies, finds types implementing the handler interfaces, and registers them. This has three costs. A missing handler is discovered on the first request that needs it, not at build time. Two handlers that both match a request are silently resolved however DI's last-registration-wins happens to fall out, not reported as an error. And runtime reflection over public types is exactly the pattern that ahead-of-time compilation and trimming cannot verify - it is a standing obstacle to shipping an AOT- and trim-clean library, which the incumbent mediator libraries in this space are not.

ADR-0003 considered source-generated registration and rejected it for the startup-cost reason: reflection is cheap once cached, so the win looked marginal. That framing missed the two costs above, which caching cannot fix - they are about *when* a mistake is caught, not how fast resolution runs afterward.

## Decision

Handler discovery moves to compile time via a Roslyn source generator.

- **Per-assembly modules.** Each handler-defining assembly emits its own registration module identified by an assembly-level attribute. The application-level generator discovers those modules by attribute and aggregates them across referenced assemblies. This keeps rebuilds incremental - an unchanged library assembly's module is not regenerated - and lets a class library ship pre-generated registrations that an application project simply aggregates, with no separate configuration step for multi-project solutions.
- **Registrations and dispatch, never direct construction.** The generator emits registration calls and a generated dispatch switch (see [ADR-0010](0010-dispatch-scope-fan-out-open-generics-ambiguity.md)) - not `new Handler(...)`. Handlers, behaviors, and stages remain container-activated, so constructor injection, decoration, and lifetime rules are exactly what they'd be under manual registration. What the generator removes is the reflection used to *find* the handler and build the dispatch wrapper, not the resolution of the handler inside it.
- **Build diagnostics.** A command or query with no matching handler is a build error, not a first-request exception. Two handlers matching the same request ambiguously is a build error (see [ADR-0010](0010-dispatch-scope-fan-out-open-generics-ambiguity.md)).
- **AOT and trim as goals.** Because discovery no longer depends on reflecting over assemblies at startup, the library can ship trim-annotated and publish ahead-of-time without warnings. Runtime assembly scanning is retained as an opt-in fallback - trim-annotated, never active by default - for the plugin scenario where handler assemblies are genuinely unknown until runtime.
- **The container is still the handler registry.** This decision does not change who owns handler lifetime or resolution - that was and remains the DI container. What changes is how the container's registration list is populated: at compile time from generated code, not at startup from reflection.

## Consequences

- **Positive:** Missing and ambiguous handlers are build failures, not production incidents.
- **Positive:** The library becomes AOT- and trim-compatible, a real differentiator against reflection-based mediator libraries.
- **Positive:** Per-assembly modules keep incremental builds fast and make class libraries self-contained for discovery - no consumer-side configuration to discover a referenced project's handlers.
- **Negative:** A source generator is a substantially larger maintenance surface than a reflection loop - it is the single largest implementation cost in this effort, weighed explicitly against a diagnostics-only analyzer (see Alternatives).
- **Negative:** Generated code is one more thing a contributor must understand to debug a registration problem. Mitigated by the generator only ever emitting registrations and dispatch, never construction logic, keeping the generated surface small and mechanical.

## Alternatives considered

- **Keep runtime reflection, add a diagnostics-only Roslyn analyzer:** Would deliver the build-time missing-handler and ambiguity diagnostics at roughly a quarter of the implementation cost of a full generator. Rejected because it does not solve AOT/trim compatibility or reflection-free runtime-typed dispatch - both of which only a generator delivers, and both of which are explicit differentiators against the incumbent alternatives.
- **Keep runtime reflection as the only mechanism (status quo):** Rejected for the reasons in Context - silent-until-runtime failures and no path to AOT.
- **Compile-time discovery via `[ModuleInitializer]` plus reflection:** Rejected - moves *when* scanning runs, not *whether* it happens; does nothing for AOT/trim and still cannot report a missing handler as a build error.
