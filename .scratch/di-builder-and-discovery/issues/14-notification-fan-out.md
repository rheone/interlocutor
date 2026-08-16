## Labels

ready-for-agent

## What to build

Let a handler subscribe to a family of domain events, and make the ordering and failure semantics of publishing something a developer can rely on rather than infer.

A developer writes an audit sink or an outbox writer that subscribes to a base event interface and receives every derived event, without registering it once per concrete event type. The notification handler contract is already contravariant; what is missing is a resolution path that uses it, since neither container matches variance for the closed types the mediator currently asks for.

Ordering and failure decisions to implement:

- **Execution order across matching handlers is explicitly unspecified**, and documented as such. An order derived from reflection or registration is a promise that breaks between runtimes, and ADR-0007's current claim of registration order must be amended.
- **The escape hatch is relative, not numeric.** Attributes naming another handler to run after or before it, topologically sorted. Relative ordering is compiler-checked - the named type must exist and be a handler - and composes without renumbering, whereas magic integers are exactly the invisible coupling this effort rejected elsewhere. Note that nested attributes are not legal C#: an attribute argument must be a constant, a type reference, or an array of those, so type references are the mechanism.
- **An ordering cycle is a build error.**
- **A catch-all handler subscribing to the base notification interface is legal** - it is the main reason to have contravariance at all - but produces an informational diagnostic naming it, so a process-wide subscriber is a deliberate choice rather than an accident.
- **Sequential publishing stops at the first exception and rethrows it unwrapped**; wrapping loses the stack for no gain. **Parallel publishing runs every handler and aggregates.** Document plainly that sequential leaves partial execution reachable.
- **The publisher stays timing-free and immediate**, running handlers in the caller's scope. The core knows nothing about commits, because a commit is only one possibility among many units of work. Deferral until after a commit, if ever wanted, belongs to the store-specific modules where the transaction behavior owns that concept - it is not built here.

The publish strategy must be selectable through the published publisher interface, not only the concrete mediator.

## Acceptance criteria

- [ ] A handler registered against a base event type or interface receives derived events, in both containers
- [ ] Handlers for the concrete type, an intermediate interface, and the base interface all run for one publish
- [ ] Execution order is documented as unspecified; no test asserts an incidental order
- [ ] Run-after and run-before attributes produce the declared relative order
- [ ] An ordering cycle produces a build error naming the cycle
- [ ] A catch-all handler works and produces an informational diagnostic
- [ ] Sequential stops at the first exception and rethrows it unwrapped; parallel runs all and aggregates
- [ ] The publish strategy is selectable through the published publisher interface
- [ ] Notification handlers run in the caller's scope and share its scoped services
- [ ] ADR-0007 is amended to match

## Blocked by

10 - Generator foundation
