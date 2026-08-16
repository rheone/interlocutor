## Labels

ready-for-agent

## What to build

Root `README.md`'s `## Deeper dive` section currently mixes user-facing and developer-facing links in one flat list:

```
## Deeper dive

- **[Full documentation index](documentation/)** — ...
- **[Architecture Decision Records](docs/adr/)** — ...
- **[Project PRD](docs/PRD.md)** — ...
```

Split it into two clearly separated, explicitly labeled subsections. README.md is primarily for package users — user docs stay primary and prominent; developer docs are visually secondary and explicitly labeled as contributor-facing, not needed to use the library:

- **User documentation** (listed first) — links to `documentation/` only.
- **Developer documentation** (explicitly labeled "for contributors and maintainers working on this repository — not needed to use the library") — links to `docs/README.md` (ticket 37) instead of the current direct `docs/adr/` and `docs/PRD.md` links, which move one hop deeper behind that new index.

## Acceptance criteria

- [ ] `## Deeper dive` has two subsections, user docs listed first/prominently
- [ ] Developer documentation subsection is explicitly labeled as for contributors/maintainers, not needed to use the library
- [ ] Direct links to `docs/adr/` and `docs/PRD.md` are removed from README.md (now reached via `docs/README.md`)

## Blocked by

37 — Create developer documentation index
