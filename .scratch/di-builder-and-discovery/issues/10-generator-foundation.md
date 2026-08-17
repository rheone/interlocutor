> **Status: done** - merged via PR #2 (`7b3eff3`), later re-merged in PR #3 (`0e1465f`).

## Labels

ready-for-agent

## What to build

Move handler discovery from runtime reflection to compile time, so a registration mistake is a build failure instead of a first-request exception.

A developer writes a command and forgets to write or register its handler. Today they find out when the request is dispatched, possibly in production. After this ticket they find out at build time, with a diagnostic naming the request type.

Scope for this ticket is the compiling project only - cross-assembly aggregation is ticket 12, and open generics are ticket 11.

Build the generator module and have it:

- Discover handler implementations and their request types in the current compilation.
- Emit a registration module the builder consumes, identified by an assembly-level attribute so ticket 12 can aggregate across assemblies.
- Emit closed dispatch wrappers, removing the runtime type-construction currently done per request type.
- Report a diagnostic when a command or query has no handler. Warning by default, escalatable to an error by the consumer.

**Hard constraint: the generator emits registrations and dispatch, never direct construction.** Handlers, behaviors, and stages must stay container-activated so constructor injection, decoration, and lifetime rules keep working. What disappears is the reflection used to *build the wrapper*, not the resolution of the handler inside it. A handler with constructor dependencies must resolve exactly as it does today.

Runtime assembly scanning survives as an explicitly opted-in fallback for plugin scenarios. It is never active by default - "it worked in development and not in the trimmed build" is precisely the surprise this library exists to avoid.

Set up the generator test project in this ticket, since every later generator ticket depends on it: snapshot the emitted code and assert the diagnostic's identifier, severity, and location.

## Acceptance criteria

- [ ] Handlers in the compiling project are discovered at build time with no explicit registration
- [ ] A command or query with no handler produces a build diagnostic naming the type
- [ ] The diagnostic's default severity is warning and a consumer can escalate it to an error
- [ ] Emitted registrations resolve handlers from the container; no handler is directly constructed
- [ ] A handler with constructor dependencies resolves and receives them
- [ ] Emitted closed wrappers replace runtime type construction on the dispatch path
- [ ] Runtime scanning still works and is only active when explicitly requested
- [ ] The generator test project exists, asserting emitted-code snapshots and diagnostic identifier, severity, and location
- [ ] Both containers work against generated registrations

## Blocked by

04 - Migrate to the builder and delete the legacy API
