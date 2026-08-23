# 06 - What `documentation/trim-and-aot-support.md` promises consumers

Type: grilling
Status: open
Blocked by: 03

## Question

What does the consumer-facing trim and AOT documentation actually say?

`CLAUDE.md` lists "Trim & AOT support" as a standing TBD placeholder blocked on ticket 17; ticket 33 of
`di-builder-and-discovery` is its documentation counterpart. This ticket settles the content, which is a
judgement call rather than transcription - the audience is someone deciding whether Interlocutor fits their
deployment, and the honest answer may be "not for your scenario."

To settle:

- **The promise, stated plainly.** Which publish modes work, on which containers, with what caveats.
- **What the consumer must do.** Whatever the sample had to do is a preview of the consumer's burden: a
  System.Text.Json source-generated context for minimal APIs, an EF Core compiled model, and schema creation
  that avoids design-time model building. These are real costs and belong in the doc, not in a footnote.
- **The runtime-scanning fallback's price.** Ticket 17 explicitly asked that this be documented - what it
  costs, and why it is off unless requested.
- **The failure mode.** What a consumer sees when they exceed the promise, and how they recognize it. If the
  decision from ticket 03 introduced a build-time diagnostic, document the diagnostic.

House style applies and matters here: this file lives in `documentation/`, the consumer-facing tree, so it
may **not** cite `docs/adr/`, `docs/PRD.md`, or any other `docs/` path to justify a design choice - state the
rationale itself as fact and link sideways to sibling `documentation/` files. HTTP status codes belong only
in `aspnetcore-integration.md`. See the house style section of `CLAUDE.md`.

## Answer

_Pending._
