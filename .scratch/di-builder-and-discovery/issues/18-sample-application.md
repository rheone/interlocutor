## Labels

ready-for-agent

## What to build

A runnable application showing the intended composition end to end, so a developer can read working code rather than reconstruct the design from prose.

There is no sample in the repository today. For a library whose selling point is predictable composition, one runnable sample is worth more than half the documentation — and it is the only place the pieces are proven to fit together in the shape a real consumer would use.

The sample is a web application with a persistence layer, exercising: a query handler, a value-returning command handler, a unit command handler, a generic handler serving several concrete request types, handlers living in a separate class library so cross-assembly discovery is demonstrated rather than described, an explicitly composed behavior chain, a pipeline stage including one that short-circuits, a notification with more than one subscriber, and the transport conversion at the edge.

It must show the *intended* usage: an explicitly composed pipeline rather than the opt-in recommended chain, since the point is that the developer chooses. Keep it small enough to read in one sitting — this is a demonstration, not a reference application.

The sample is also the subject of the ahead-of-time publish gate in ticket 17, so it must stay AOT-clean.

## Acceptance criteria

- [ ] A runnable web sample builds and starts
- [ ] It exercises query, value-returning command, unit command, and generic handlers
- [ ] Handlers live in a separate class library, discovered with no assembly list
- [ ] The behavior chain is composed explicitly, not via the recommended-chain shortcut
- [ ] A pipeline stage is present, including one that short-circuits
- [ ] A notification with more than one subscriber is published and handled
- [ ] Transport conversion is shown at the edge
- [ ] The sample is small enough to read in one sitting
- [ ] It is excluded from packaging

## Blocked by

04 — Migrate to the builder and delete the legacy API
07 — StageResult and short-circuit
14 — Notification fan-out
