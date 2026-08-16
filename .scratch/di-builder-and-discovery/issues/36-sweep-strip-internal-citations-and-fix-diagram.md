## Labels

ready-for-agent

## What to build

`documentation/` (the library-consumer-facing doc tree) currently leaks maintainer-facing content: 9 of its 14 files cite ADRs or `docs/PRD.md` directly in prose (e.g., "See ADR-0008 for the compile-time discovery mechanism"), and `documentation/index.md` links straight to `docs/adr/` and `docs/PRD.md`. A library consumer has no reason to know what an ADR is or care about this repo's internal decision-tracking. This is a consolidated sweep across the whole tree — same mechanical edit repeated, not independently valuable per-file slices.

**Strip the citation, keep the rationale.** The "why" prose (e.g., why auditing sits outside authorization) stays — a consumer composing their own pipeline needs that reasoning. Only the citation/link to the internal decision record goes. Three patterns:

1. **Citation with rationale to fold in as stated fact** — delete the "See ADR-XXXX for..." clause; if it explained *why*, restate the reasoning directly rather than as a citation.
2. **Citation with no rationale attached, or rationale already stated elsewhere in the sentence** — just delete the trailing citation clause.
3. **Glossary/Links inline citations** ("short definition plus a link" — deleting the link leaves a dead end) — redirect sideways to the sibling `documentation/` file that already covers the topic, never to `docs/`.

Files and locations needing this treatment:

- `builder-and-registration.md:3` — ADR-0008, ADR-0009 (pattern 1)
- `handler-selection-and-dispatch.md:3` — ADR-0008, ADR-0010 (pattern 1)
- `mediator-lifetimes-and-scope.md:3` — ADR-0006, ADR-0011 (pattern 1)
- `messages-and-result-types.md:3` — ADR-0001, ADR-0013 (pattern 1); `messages-and-result-types.md:330` — ADR-0013 (pattern 2, reasoning already stated in the same file)
- `notifications-and-fan-out.md:3` — ADR-0007, ADR-0010 (pattern 1)
- `pipeline-and-behaviors.md:3` — ADR-0002, ADR-0009 (pattern 1); `pipeline-and-behaviors.md:288` — ADR-0009 (pattern 2)
- `transactions-and-store-integration.md:3` — ADR-0011, `docs/PRD.md` §9.2 (pattern 1)
- `trim-and-aot-support.md:17` — ADR-0008 in its `## Links` list (pattern 3 — redirect to `handler-selection-and-dispatch.md`)
- `glossary.md:17` (IOperationOutcome row, ADR-0013 — pattern 3, redirect to `messages-and-result-types.md`); `glossary.md:81` (IMediator row, "the mediator-lifetime ADR" — pattern 3, redirect to `mediator-lifetimes-and-scope.md`)
- `testing.md:3` — `docs/PRD.md` §16 (pattern 2)

**Update `documentation/index.md`:**

- Remove the "Quick navigation" section's direct `docs/adr/` and `docs/PRD.md` links (they're the exact thing the new rule below forbids).
- Update the intro line to point at `docs/README.md` (ticket 37) instead of the bare `docs/` folder.
- Add a new rule to "Content rules": **No internal design-record citations** — never cite or link to `docs/adr/`, `docs/PRD.md`, `docs/TODO.md`, or any other `docs/` path to justify a design choice. State the rationale itself, as fact. `docs/` is internal maintainer documentation; a consumer reading this tree has no reason to open it. If a claim needs a pointer, link sideways to the sibling `documentation/` file that already covers it, never to `docs/`.

**Fix the missing diagram.** `builder-and-registration.md`'s "Cross-Assembly Aggregation" section has only a plain directory-listing code block — its original ticket (25) required an inline mermaid diagram showing generator module aggregation across assemblies, which never shipped. Add one (keep the existing code block too), matching `pipeline-and-behaviors.md`'s existing diagram style (`graph TD`, `<br/>` multi-line labels, light-blue/green/pink `style` palette). Show: the domain assembly's handler → its generated module; the entry-point assembly's `ScanEntryAssembly()` → its generated module, which discovers and aggregates the domain assembly's module via the project reference; aggregated result flows into the DI container. Add one caption sentence stating that aggregation follows project references transitively, so scanning the entry assembly alone is sufficient regardless of how many referenced assemblies contain handlers.

## Acceptance criteria

- [ ] No `documentation/*.md` file (other than `index.md`'s single permitted top-of-file wayfinding line) links to or mentions `docs/adr/`, `docs/PRD.md`, or `docs/TODO.md`
- [ ] All rationale prose is preserved as stated fact, not deleted wholesale
- [ ] `glossary.md`'s two entries and `trim-and-aot-support.md`'s Links entry redirect sideways to sibling `documentation/` files instead of `docs/`
- [ ] `index.md`'s house conventions include the new no-citation rule, and its own two `docs/` links are removed
- [ ] `builder-and-registration.md` has one inline mermaid diagram showing cross-assembly module aggregation
- [ ] Every code example still compiles against the shipped API (re-verify nothing broke during the sweep)

## Blocked by

None — can start immediately
