## Labels

ready-for-agent

## What to build

Make a multi-project solution work with no extra configuration.

A developer puts their handlers in a class library and references it from a web application. Handlers are discovered and registered with a single configuration call in the application, and neither project lists the other's assemblies anywhere.

Each handler-defining assembly runs the generator and emits its own registration module identified by an assembly-level attribute. The application-level generator discovers those modules by attribute across referenced assemblies and aggregates them. This keeps rebuilds incremental - changing one library regenerates only that library's module - and lets a class library ship pre-generated registrations to consumers.

The alternative of having only the entry project walk every referenced assembly's metadata was considered and rejected: it re-scans the whole reference graph on every build and cannot see handlers in assemblies it does not directly reference.

Handlers in assemblies loaded at runtime rather than referenced at compile time remain the province of the explicitly opted-in runtime scanning fallback.

## Acceptance criteria

- [ ] Handlers in a referenced class library are discovered with no assembly list and no extra configuration
- [ ] Each handler-defining assembly emits its own registration module identified by an assembly-level attribute
- [ ] The application-level generator aggregates modules found across referenced assemblies
- [ ] Three-level referencing works: application references library A which references library B
- [ ] Changing one library does not force regeneration of the others
- [ ] A missing-handler diagnostic still fires for a request declared in one assembly whose handler is absent everywhere
- [ ] Duplicate registration does not occur when an assembly is referenced through more than one path
- [ ] Both containers work against aggregated registrations

## Blocked by

10 - Generator foundation
