## Labels

ready-for-agent

## What to build

Make runtime-typed dispatch reflection-free.

Ticket 09 delivered dispatch of an untyped request using reflection, which unblocked outbox and queue consumers. Replace that implementation with a generated switch on the runtime type dispatching into the strongly-typed path. Behavior visible to the caller is unchanged; what changes is that the hot path no longer reflects, which is also what makes ahead-of-time compilation possible in ticket 17.

The reflection implementation is retained only under the explicitly opted-in runtime scanning fallback, where the set of request types is not known at build time.

A request type the generator did not see — reachable when runtime scanning is off and a type arrives from an unreferenced assembly — must still fail with the same clear, named exception rather than falling through the switch silently.

## Acceptance criteria

- [ ] Runtime-typed dispatch is backed by a generated type switch
- [ ] Every behavior asserted in ticket 09 still holds, unchanged from the caller's perspective
- [ ] No reflection remains on the dispatch path when runtime scanning is off
- [ ] A request type the generator did not see produces the same named exception
- [ ] The reflection implementation still serves the runtime scanning fallback
- [ ] Generator tests cover the emitted switch, including the zero-handler and single-handler cases

## Blocked by

09 — Runtime-typed dispatch
10 — Generator foundation
