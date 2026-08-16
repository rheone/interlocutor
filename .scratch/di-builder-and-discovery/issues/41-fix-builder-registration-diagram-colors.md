## Labels

ready-for-agent

## What to build

`documentation/builder-and-registration.md`'s "Cross-Assembly Aggregation" `graph TD` diagram styles four nodes with hardcoded hex fills (`style Scan fill:#ADD8E6`, `style DomainModule fill:#90EE90`, `style AppModule fill:#90EE90`, `style Container fill:#FFB6C6`) and no matching text-color override. GitHub switches its Mermaid theme with the site's light/dark toggle, but a literal `fill:` still wins over the theme's node background while the node's text color continues to derive from the theme - so a light fill paired with theme-derived light text in dark mode risks becoming unreadable. This is a readability bug, not a cosmetic nit.

**Fix:** replace the four `style` lines with `classDef` declarations that set both `fill` and `color` explicitly (e.g. a `classDef` per semantic role - input/generated/output - each with an explicit background *and* a text color that stays legible against it), applied via `class` or the `:::` shorthand. Keep the color-coding itself (it carries real meaning: distinguishing the scan entry point, generated modules, and the DI container) - only the theme-safety mechanism changes.

**Verify by rendering**, not just by reading the syntax: confirm the diagram is legible in both GitHub's light and dark modes before calling this done - GitHub doesn't publish exact Mermaid theme-handling behavior, so this needs an actual render check (e.g. a scratch gist or PR preview), not just a syntax read-through.

## Acceptance criteria

- [ ] No `style nodeId fill:#hex` lines remain without an explicit paired text color
- [ ] Diagram renders correctly via `node tools/validate-mermaid.mjs` (from `.claude/skills/mermaid-diagram-generator/`)
- [ ] Diagram confirmed legible in both GitHub light and dark rendering (actual render check, not just syntax validation)
- [ ] The color-coding's semantic meaning (scan entry point vs. generated module vs. DI container) is preserved
- [ ] Diagram type remains `graph`/`flowchart` - no diagram-type change in this ticket

## Blocked by

None - can start immediately
