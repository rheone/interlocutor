# mermaid-diagram-generator

Generates any Mermaid diagram type - flowchart through the newest beta/experimental
types - as standalone `.mermaid`/`.mmd` files or markdown-embedded ` ```mermaid `
blocks, targeting Mermaid v11.16.1.

## Structure

```
mermaid-diagram-generator/
├── SKILL.md                       # Entry point - diagram-type index, decision table,
│                                   #   output-format patterns, self-check checklist
├── README.md                      # This file
└── references/
    ├── <slug>.md                  # One file per diagram type (30 total), each with
    │                               #   frontmatter (status/version/keyword/source)
    │                               #   and the same 10-section template
    └── general/
        ├── configuration.md       # Global config resolution order
        ├── directives.md          # %%{init: {...}}%% syntax
        ├── theming.md             # Built-in themes and themeVariables
        ├── math.md                # KaTeX-based math rendering
        ├── accessibility.md       # accTitle/accDescr, generated ARIA output
        └── layout.md              # Layout engines (dagre/elk/etc.)
```

### Diagram type coverage (30)

| Status | Count | Types |
|---|---|---|
| 🟢 Stable | 13 | flowchart, sequence, class, state, gitgraph, user-journey, gantt, pie, quadrant, requirement, mindmap, timeline, zenuml |
| 🟡 Beta | 14 | sankey, treemap, xy-chart, block, packet, kanban, architecture, radar, venn, ishikawa, wardley, cynefin, treeview, swimlanes |
| 🔴 Experimental | 3 | entity-relationship, c4, event-modeling |

Every `references/<slug>.md` file is independently frontmatter'd with its own
`status`, `mermaid_version_introduced`, `mermaid_version_verified`, and verified
`keyword` - statuses and keywords were confirmed against mermaid.js.org's raw page
source (not just WebFetch summaries, which were found to hallucinate syntax on
several newer pages during authoring), so don't assume a diagram's `-beta` suffix
convention without checking its file (e.g. `sankey`/`xychart`/`block` dropped the
suffix; `venn-beta`/`ishikawa-beta`/`wardley-beta` kept it).

## Quick start

Just ask, in the host conversation, for a diagram - this skill activates on
requests like "draw a flowchart for...", "sequence diagram of...", or "make a
Mermaid diagram showing...". It will:

1. Match the request against `SKILL.md`'s decision table to pick a diagram type
   (asking first if 2-3 types plausibly fit).
2. Read that type's `references/<slug>.md` before writing any syntax.
3. Produce the diagram as a `.mmd` file (default for standalone output), a
   `.mermaid` file (if you name that extension), or a ` ```mermaid ` fenced block
   embedded in a markdown file (default when the target is a doc).
4. Call out explicitly when the chosen type is beta or experimental, since its
   syntax is more likely to shift on a future Mermaid upgrade.

A programmatic validator (`tools/validate-mermaid.mjs`) tests every example against Mermaid
v11.16.1 - structure checks, grammar validation, and full rendering. All 68 blocks pass or are
intentionally skipped (plugin-dependent types like ZenUML, experimental types with parser issues).
For rendering details and integration with your workflow, see [`tools/README.md`](tools/README.md).

## Updating

mermaid.js.org content shifts between Mermaid releases. When bumping the pinned
version (currently v11.16.1, in `SKILL.md`'s frontmatter):

1. Re-fetch each changed diagram's page and diff against its `references/<slug>.md`
   - pay special attention to `keyword` (some beta types have dropped/added
   `-beta` suffixes between releases) and any new `mermaid_version_introduced`
   feature gates called out mid-file.
2. Bump `mermaid_version_verified` and `last_verified` in every file actually
   re-checked - don't bulk-update dates for files you didn't re-verify.
3. Re-run the verification pass below.

## Verification

Automated validation with the bundled tool - instant structure checks, or full rendering with puppeteer:

```bash
# Structure + keyword + version checks (no dependencies; ~1s)
node tools/validate-mermaid.mjs --mode none

# Parse-only check (fast; mermaid + jsdom; ~2-5s)
node tools/validate-mermaid.mjs --mode parse

# Full render validation (mermaid + puppeteer; ~15-25s)
node tools/validate-mermaid.mjs
```

All 64 diagram examples pass (4 intentionally skipped: zenuml and event-modeling, which require external plugins or have known parser issues in v11.16.1). See [`tools/README.md`](tools/README.md) for detailed usage, manual validation fallbacks, and CI integration examples.
