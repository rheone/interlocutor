---
name: audit-naming
description: Find ambiguous, vague, generic, clashing, and overloaded identifier names across a codebase, rank the worst offenders, and orchestrate whichever renames get approved — code, filenames, and docs kept in sync.
argument-hint: "Path or git ref to scope the audit (default: whole codebase); add --locals to also check variables/lambdas/constants/parameters"
disable-model-invocation: true
---

# Audit Naming

Find names that make the codebase harder to read than it needs to be, then fix only the ones a human signs off on. Never flag a name for being merely unfamiliar — a name earns a finding by being ambiguous, vague, generic-to-the-point-of-meaningless, or actually colliding with another meaning, not by failing to match a rewrite you'd have chosen yourself. A name matching a well-known pattern, framework idiom, or established type in its ecosystem is never a finding, however generic it reads in isolation — see [`references/well-known-patterns.md`](references/well-known-patterns.md).

## Tiers

Every finding lands in exactly one tier; report worst tier first, and within a tier, most-referenced first:

1. **Clash** — one identifier, genuinely different concepts (distinct types/functions/modules reusing the same name in a way that could mislead a reader or a search). Highest correctness risk.
2. **Overload** — one identifier doing double duty across contexts: a single name standing in for two related-but-distinct responsibilities.
3. **Ambiguous/vague** — a name that doesn't tell a reader what the thing is or does, weighted by how much of the codebase depends on it (public surface and reference count outrank a private, lightly-used sibling with the same problem).
4. **Generic-but-unambiguous** — bland (`Data`, `Info`, `Common`) but with no competing meaning anywhere else in the codebase. Cosmetic, lowest priority.
5. **Local** *(only when `--locals` is on)* — variables, lambda parameters, constants, function parameters. Always sits below all four tiers above regardless of an individual finding's own severity — a locally-confusing lambda parameter never outranks a real clash.

Repeat instances of the same underlying naming choice — six unrelated types all trailing off into a bare `Data`, say — are one **family** in the report, not six findings. Offer options at the family level, but let the human drill into individual members of it.

## Exemption authority

Before anything is classified, filter it against these sources, in order, and never flag a match:

1. [`references/well-known-patterns.md`](references/well-known-patterns.md) — the ecosystem-specific patterns this skill ships with.
2. An existing project glossary, if the codebase already has one (check the root and `docs/`/`documentation/` for `GLOSSARY.md`, `TERMINOLOGY.md`, `glossary.md`) — every term it defines is exempt as-is; never duplicate its content into anything this skill writes.
3. `NAMING_GLOSSARY.md` at the codebase root, if present — this skill's own record of prior "leave as-is" decisions, project-specific wildcard exemptions, and, only when no broader glossary exists, project terminology. Exemption patterns support `*`/`?` wildcards matched against the bare identifier name.

## Steps

1. **Establish run configuration.** Read the invocation for a scope (a path or git ref; default the whole codebase) and a `--locals` flag. If neither was given, ask both in one question rather than assuming either. Done when: scope and locals-inclusion are both known.

2. **Establish scan context.**
   - *Ecosystem(s):* find manifest files (`*.csproj`/`*.sln`, `package.json`, `go.mod`, `pyproject.toml`/`setup.py`, `pom.xml`/`build.gradle*`, `Cargo.toml`, `Gemfile`, `composer.json`, and the like) to determine which language(s) are in play — a monorepo can have several at once. No recognized manifest means falling back to the generic, cross-language section of the exemption reference and a case-convention heuristic, not refusing to run.
   - *Exemption authority:* assemble the merged set from the three sources above. Note whether `NAMING_GLOSSARY.md` exists yet — it may not, and that's fine; it's only created in step 5 if there's actually something to write into it.
   - *Tracking convention:* check the codebase's own `CLAUDE.md`/`AGENTS.md`/`CONTRIBUTING.md`/`README` for a documented issue/ticket workflow. If one exists, skim its currently-open items for anything naming-related — flag those as already-tracked in the report instead of presenting them as fresh findings.
   Done when: ecosystem(s), the merged exemption set, and the tracking convention (or confirmation there isn't one) are all known.

3. **Scan.** Dispatch a read-only subagent to search with `rg` for ecosystem-appropriate declaration syntax across the scoped area, filter every candidate against the exemption authority, and classify survivors into a tier. Check `ToolSearch` for an available LSP-backed capability (find-references / rename-symbol / go-to-definition); prefer it over raw text matching wherever it can confirm a real declaration against an incidental substring hit. Each surviving candidate needs: identifier, kind, location(s) and a reference count, tier, a one-line rationale, and — for a clash or overload — the competing meanings it's caught between. Done when: the candidate list, with families already grouped, comes back.

4. **Rank and report.** Sort worst-tier-first, most-referenced-first within a tier; list already-tracked items separately from fresh findings. Present the ranked, numbered list. Done when: the report has been shown.

5. **Collect selections.** For each offender or family the human wants to act on, offer several concrete replacement-name options plus an explicit "leave as-is" option — never assume a rename is wanted just because a name was flagged. The moment "leave as-is" is picked, record it into `NAMING_GLOSSARY.md` immediately — creating the file at the codebase root from the template below if step 2 found nothing already filling that role — rather than batching it until the end, so the decision survives even if the session stops here. Done when: every offender/family the human engaged with carries either a chosen replacement name or a recorded exemption.

6. **Orchestrate each accepted fix.** Before touching anything, run a full-codebase reference search for the exact identifier — never limited to step 1's scope — via the LSP capability if available, else `rg`, to build its **impact list**: every call site, the matching filename, and every doc/comment mention. A rename touching code outside the original scope is expected and correct, not a scope violation. Then:
   - If step 2 found a documented tracking convention, file the fix there, in that convention's own format — don't substitute a shape of your own invention.
   - Otherwise, enter plan mode: present the impact list as the plan, and execute only on approval. Offer to also save the plan as a standalone file if a durable record is wanted beyond the chat transcript.
   - Exception: a rename confined to one file with zero doc/comment hits may be offered as an immediate edit instead of a plan — the ceremony isn't worth it for something that small.
   Done when: every accepted fix has either landed as an edit or been filed as a plan/ticket.

7. **Summarize.** Report what happened to each offender/family engaged with in step 5: fixed now, filed as a plan/ticket, or left as-is (and where that decision is now recorded). Done when: the summary has been given.

## `NAMING_GLOSSARY.md` template

Used only when step 2 found nothing already filling the exemption/glossary role:

```markdown
# Naming Glossary

Project-specific naming decisions, maintained by `audit-naming` (`/audit-naming`).

## Exemptions

Names or patterns that look ambiguous/generic/vague but are deliberate — never re-flag these. Patterns support `*`/`?` wildcards matched against the bare identifier name.

| Pattern | Reason |
|---|---|

## Project glossary

Project-specific terms and concepts.

| Term | Definition |
|---|---|
```
