# Validation Guide for mermaid-diagram-generator

This directory contains `validate-mermaid.mjs`, a replayable validator for all 68 Mermaid diagram examples in the skill, pinned to **Mermaid v11.16.1**. The script performs structural checks, keyword drift detection, version pin verification, and optional grammar/rendering validation.

## Quick start

### Scripted validation

Prerequisites: Node.js 18+ (v24.19.0 / npm 11.17.0 confirmed on this host).

```bash
# Run all checks: structure + keyword + version + render (full validation)
node tools/validate-mermaid.mjs

# Parse-only validation (fast, no browser)
node tools/validate-mermaid.mjs --mode parse

# Structural checks only (instant, no dependencies)
node tools/validate-mermaid.mjs --mode none

# Validate a single diagram type
node tools/validate-mermaid.mjs --only architecture,treeview

# Machine-readable JSON output
node tools/validate-mermaid.mjs --json | jq .

# Remove the temp dependency cache (outside the repo)
node tools/validate-mermaid.mjs --clean
```

### What each mode does

| Mode                 | What it checks                     | Deps required      | Time    | Best for                                       |
| -------------------- | ---------------------------------- | ------------------ | ------- | ---------------------------------------------- |
| **render** (default) | Grammar + layout + icon resolution | mermaid, puppeteer | ~15–25s | Full validation; catches the most bugs         |
| **parse**            | Grammar only (no rendering)        | mermaid, jsdom     | ~2–5s   | CI/quick checks; no browser system libs needed |
| **none**             | Structure, keywords, version pins  | none               | <1s     | Offline checks; pre-merge validation           |

### Understanding the output

```
summary mode=parse blocks=68 passed=68 skipped=0 problems=0
OK - everything checks out
```

- **blocks**: total diagram examples discovered (68 total; includes 2 zenuml blocks)
- **passed**: successfully validated
- **skipped**: explicitly annotated with `<!-- mermaid-validate: skip ... -->` - currently nothing is fully skipped; ZenUML's 2 blocks are `parse-only` instead (validated in `--mode parse`, not attempted in `--mode render` - see "Per-block annotations" below)
- **problems**: failures that need fixing

On failure:

```
parse - 1 problem(s)
  FAIL references/architecture.md:60
        parse failed: Error text here
        [source lines of the failing block shown]
```

Click [source line reference](../references/architecture.md#L60) to jump to the block.

## Common scenarios

### "I just cloned the repo. Do I run this?"

Yes, to spot-check examples or CI/pre-commit. But the default run (`node tools/validate-mermaid.mjs`) downloads ~180MB of Chromium (via puppeteer), so consider `--mode parse` for a faster first run:

```bash
node tools/validate-mermaid.mjs --mode parse
```

If that passes, the full render run is optional (it catches layout/icon issues the parser misses, but those are rare with stable diagram types).

### "I updated the Mermaid version pin in SKILL.md. How do I verify all examples still work?"

1. Update `metadata.mermaid_version` in `SKILL.md` to the new version.
2. Update `mermaid_version_verified` and `last_verified` in each diagram reference file you actually re-checked (don't bulk-update dates for files you didn't inspect).
3. Run:
   ```bash
   node tools/validate-mermaid.mjs --clean   # remove the old mermaid version from cache
   node tools/validate-mermaid.mjs --mode parse  # fast check
   ```
4. If parse passes, run the full render mode:
   ```bash
   node tools/validate-mermaid.mjs
   ```
5. On failure, fix the example or update your frontmatter notes with the breaking change.

### "One diagram type keeps failing. Do I delete it?"

No - first check whether the example itself is wrong before assuming it's an upstream Mermaid
bug. Event Modeling's examples looked "obviously correct" but were missing the mandatory
`tf`/`timeframe` keyword on every line; testing candidate syntax directly against the pinned
parser (see Option B below) found the fix in minutes. Only annotate a block as broken once
you've confirmed - by testing, not by inspection - that no known-correct syntax parses:

```markdown
<!-- mermaid-validate: skip reason="<diagram> is experimental in v11.16.1 and has parser issues" -->
```

The manual checklist in `SKILL.md` still applies when a user generates a new diagram of that type - they can spot-check it by hand or paste it into [mermaid.live](https://mermaid.live) (noting it may run a different Mermaid version).

## Manual validation (no script)

If you want to validate one or two blocks by hand without installing anything:

### Option A: Use mermaid.live

1. Copy the diagram source code (the contents of the ` ```mermaid ` fence).
2. Paste it into https://mermaid.live and click "Parse" or "Render".
3. Note: mermaid.live may run a different Mermaid version than v11.16.1, so results may vary.

### Option B: One-off parse check (15 seconds, no browser)

```bash
cd /tmp
npm install --no-save mermaid@11.16.1 jsdom
```

Then use the Node REPL to test a single block:

```javascript
const { JSDOM } = require("jsdom");
const mod = require("mermaid");
const mermaid = mod.default ?? mod;
mermaid.initialize({ startOnLoad: false, securityLevel: "loose" });

// Paste your diagram code here
const code = `flowchart TD\nA --> B`;

try {
   await mermaid.parse(code);
   console.log("✓ Parse succeeded");
} catch (e) {
   console.log("✗ Parse failed:", e.message);
}
```

### Option C: Render locally (requires Chromium, ~60 seconds)

```bash
npm install --no-save puppeteer@latest
# Then see the "One-off parse check" above, but use mermaid.render() instead:
const { svg } = await mermaid.render('diagram-id', code);
console.log(svg.includes('error') ? '✗ Render failed' : '✓ Render succeeded');
```

## Per-block annotations

Some diagram blocks have special requirements and are annotated:

```markdown
<!-- mermaid-validate: skip reason="requires a plugin the validator doesn't install/register" -->
<!-- mermaid-validate: parse-only reason="validator registers @mermaid-js/mermaid-zenuml for --mode parse but not yet for --mode render" -->
<!-- mermaid-validate: keyword-exempt reason="C4 family diagrams use C4Context|C4Container|..." -->
```

- **skip**: don't validate this block at all (for a plugin-dependent type the validator hasn't wired up, or a type with genuine, confirmed parser issues - confirmed by testing, see above, not assumed from the reason alone)
- **parse-only**: validate grammar, but skip the render phase (currently used by ZenUML's two blocks - the validator registers its plugin for `--mode parse` via jsdom, but `--mode render` doesn't register it against the puppeteer/browser bundle yet, so a full render would fail there even though the grammar is confirmed valid)
- **keyword-exempt**: skip the keyword drift check (for diagram families like C4 that support multiple starting keywords)

## Troubleshooting

### "npm install failed in /path/to/cache"

The validator tried to install mermaid/jsdom/puppeteer and it failed. Check that:

1. You have npm 7+ installed (`npm --version`)
2. Internet access is available
3. You have write permissions to `$TMPDIR`

Then try manually:

```bash
cd "$TMPDIR/mermaid-validate-11.16.1"          # Windows: %TEMP%\mermaid-validate-11.16.1
npm install mermaid@11.16.1 jsdom
```

### "mermaid not found in cache after npm install"

This can happen on Windows if npm's `--prefix` flag is ignored. Workaround:

```bash
cd "$TMPDIR/mermaid-validate-11.16.1"          # Windows: %TEMP%\mermaid-validate-11.16.1
npm install --no-save mermaid@11.16.1 jsdom puppeteer
```

### "Puppeteer failed to launch"

Puppeteer needs system libraries (Linux) or Chromium (Windows/macOS). On Windows, this usually just works. On Linux in a container without X11:

1. Try `--mode parse` instead (no browser needed).
2. Or install Chromium system libs: `apt-get install -y chromium-browser`.

## Cache cleanup

Dependencies are cached in the OS temp directory (`$TMPDIR/mermaid-validate-11.16.1/`) outside the repo, so `git status` stays clean. To free space:

```bash
node tools/validate-mermaid.mjs --clean
```

This does not affect the repo at all - it only deletes the temp cache. Safe to run anytime.

## Integration with your workflow

### Pre-commit hook

```bash
#!/bin/bash
cd "$(git rev-parse --show-toplevel)/skills/mermaid-diagram-generator"
node tools/validate-mermaid.mjs --mode parse || exit 1
```

### CI pipeline

```yaml
- name: Validate Mermaid examples
  run: |
     cd skills/mermaid-diagram-generator
     node tools/validate-mermaid.mjs --mode parse
```

## When render mode is slow or unavailable

If puppeteer takes too long or the host lacks Chromium, use `--mode parse`:

```bash
node tools/validate-mermaid.mjs --mode parse
```

This catches grammar errors (most bugs) but misses layout/icon resolution issues. For a release or after a Mermaid version bump, do a full render pass at least once to be thorough.
