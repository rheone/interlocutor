## Labels

ready-for-agent

## What to build

A prefactor: make the change easy before making the easy change. Today the Microsoft DI and Autofac packages each carry an independently maintained copy of the same handler-scanning logic and their own separate options type. Every subsequent ticket in this effort would have to make its change twice and keep the two in sync.

Introduce a container-neutral dependency-injection abstractions module that owns a neutral registration record — service type, implementation type, lifetime, and a kind discriminator distinguishing a handler from a behavior from a default that may be overridden. Both container packages become translators from that record into their own primitives.

**This ticket changes no observable behavior.** The public registration API, the set of things registered, their lifetimes, and dispatch all stay exactly as they are. Existing tests should pass without modification — if a test needs changing, that is a signal the prefactor overreached.

## Acceptance criteria

- [ ] A new container-neutral registration module exists, depending only on the mediator abstractions module
- [ ] The neutral registration record expresses service type, implementation type, lifetime, and registration kind
- [ ] Both container packages translate from the neutral record; neither retains its own scanning loop
- [ ] The public registration API is unchanged
- [ ] Every existing test passes with no modification
- [ ] The architecture test project asserts the new module's dependency boundary

## Blocked by

None — can start immediately
