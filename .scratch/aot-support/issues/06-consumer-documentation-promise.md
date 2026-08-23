# 06 - What `documentation/trim-and-aot-support.md` promises consumers

Type: grilling
Status: resolved
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

Written: [`documentation/trim-and-aot-support.md`](../../documentation/trim-and-aot-support.md).

- **The promise, stated plainly.** Trim-compatible, verified end to end, on every container tested. Not
  Native AOT - and stated as a hard "does not work" rather than an unsupported-but-maybe-fine gap, because
  that is what was verified.
- **The failure mode.** Documented explicitly and up front, in its own paragraph: attempting `PublishAot=true`
  produces a clean, warning-free build that then fails every dispatch at runtime. No build-time diagnostic
  exists for this today - that would require the library to detect at compile time that a consumer intends
  to publish ahead-of-time, which nothing in the shipped decision (Option A) adds. A consumer discovers the
  gap by attempting the publish and running the result, exactly as this session discovered it. Making that
  failure a build-time diagnostic instead is exactly the kind of consumer-facing improvement Option B's
  generator work would deliver alongside true support - not something to bolt onto Option A alone.
- **What the consumer must do.** **Correction, written after this ticket was first marked resolved:** the
  original answer here claimed the EF Core compiled-model burden was AOT-specific and had been dropped once
  the decision landed on trim only. That was wrong, and the CI gate (ticket 05) is exactly what caught it -
  a `dotnet publish -p:PublishTrimmed=true` of the sample, actually run rather than only built, crashed with
  `MissingMethodException` on the very first request, from EF Core's own model validation reflectively
  constructing a value comparer. `ExecuteSqlRaw` (not only `EnsureCreated()`) triggers it - it fires the
  first time *any* database operation runs, which is unavoidable for an app that uses the database at all.
  The compiled model is genuinely needed for trim, not merely AOT, and is back in the sample and documented
  as a real, load-bearing cost in "Known limitations." A `JsonSerializerContext` for minimal API bodies
  remains required too, confirmed by the same real trimmed run rather than assumed.
- **The runtime-scanning fallback's price.** Documented in its own section: genuinely unsafe to trim by
  design (that is the point of calling it), every entry point carries `RequiresUnreferencedCode` so a
  trimmed publish that calls one gets a named warning rather than a silent removal, and none of it runs
  unless a consumer explicitly opts in.

House style followed: no citation to `docs/adr/` anywhere in the shipped file - the reasoning (value-type
generics, no JIT under Native AOT) is stated as fact, not attributed to ADR-0016. No HTTP status codes
appear; the only status-code-adjacent content is a generic mention of `ProblemDetails`, which names no code.
