## Labels

ready-for-agent

## What to build

`documentation/mediator-lifetimes-and-scope.md` covers three dispatch contexts in prose and code samples - ASP.NET Core web request (one scope per HTTP request, implicit), background/hosted service (explicit scope creation required), and minimal API - but has no diagram tying scope lifetime to the mediator resolution across them.

Add sequence diagram(s) showing, per context, how a scope is obtained (or already exists), how `IMediator`/`ISender`/`IPublisher` is resolved from it, and when the scope ends - using `activate`/`deactivate` to make the scope's lifetime visually explicit rather than just described in prose. Since the underlying "obtain scope → resolve mediator → dispatch → scope ends" shape repeats across all three contexts with only the *how the scope is obtained* step differing, prefer one diagram with the three contexts as parallel branches (or three short diagrams if that reads more clearly once drafted) over restating the full flow three times - use judgment on whichever reads clearest once actually laid out.

This diagram is a genuine value-add over the existing code samples specifically because it makes scope *lifetime* (not just the resolution call) visible - pick whichever layout best foregrounds that, since it's the one thing the code samples alone don't show.

**Verify by rendering** in both GitHub light and dark modes before calling this done.

## Acceptance criteria

- [ ] Diagram(s) cover all three contexts: ASP.NET Core web request, background/hosted service, minimal API
- [ ] Scope lifetime (not just mediator resolution) is visually explicit, e.g. via `activate`/`deactivate`
- [ ] Diagram(s) render correctly via `node tools/validate-mermaid.mjs` and are confirmed legible in both GitHub light and dark rendering
- [ ] Any manual coloring uses `classDef` with explicit `fill` and `color`
- [ ] Existing code samples are kept (the diagram complements them, not a replacement) unless genuinely redundant prose is trimmed

## Blocked by

None - can start immediately
