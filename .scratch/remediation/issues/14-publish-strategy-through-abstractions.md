## Labels

ready-for-agent

## What to build

The notification publish strategy is unreachable through the published interfaces — the strategy-taking overload exists only on the concrete mediator, so consumers injecting the abstraction cannot select parallel dispatch. This defeats the interface segregation the package is built around.

Two documentation claims in this area are also false and must be reconciled with the implementation:

- The publisher's documentation says publishing returns immediately without waiting for handlers. It does not; awaiting the call awaits every handler.
- The parallel wrapper claims exceptions aggregate. Awaiting the underlying task rethrows only the first, silently discarding the rest.

Decide, for each, whether to change the code or the documentation, and make them agree.

## Acceptance criteria

- [ ] The publish strategy is selectable through the published interface, not just the concrete type
- [ ] Sequential dispatch invokes handlers in order and stops at the first exception
- [ ] Parallel dispatch invokes every handler, with exception surfacing that matches its documentation
- [ ] Cancellation is honoured, not merely passed through
- [ ] Publisher documentation matches actual awaiting behavior
- [ ] Tests are red before green

## Blocked by

- 13-registration-options
