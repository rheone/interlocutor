## Labels

ready-for-agent

## What to build

A `documentation/sample-app-walkthrough.md` giving a fuller, guided tour built on the runnable sample application from ticket 18. This ticket stays blocked until ticket 18 ships — it walks through *actual* sample code, not a hypothetical one. This is deliberately separate from the README's Quick Start (ticket 22), which stays minimal; this doc is the deeper walkthrough for a reader who wants to see the composition model in a real, runnable project.

Cover, once unblocked:

- A guided tour of the sample's structure and what each piece demonstrates (per ticket 18: query handler, value-returning command handler, unit command handler, a generic handler serving several request types, cross-assembly handler discovery, an explicitly composed behavior chain, a short-circuiting pipeline stage, a multi-subscriber notification, transport conversion at the edge).
- Why the sample composes its pipeline explicitly rather than using the recommended-chain shortcut, and what that teaches a reader about making their own composition choices.

## Acceptance criteria

- [ ] Walkthrough references the actual sample application code from ticket 18, not a reconstructed example
- [ ] Every piece the sample demonstrates (per ticket 18's acceptance criteria) has a corresponding explanation here
- [ ] The explicit-composition-over-recommended-chain choice is explained
- [ ] Follows house conventions from ticket 21

## Blocked by

21 — Establish documentation/ directory; retire CONTEXT.md
18 — Sample application
