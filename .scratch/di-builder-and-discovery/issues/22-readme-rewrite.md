## Labels

ready-for-agent

## What to build

Rewrite `README.md` as a lightweight landing page, not a reference manual. The current README is 429 lines covering everything from Quick Start to Interface Segregation to Performance - most of that content moves into `documentation/` (tickets 23–32) and the README becomes an entry point that links out.

From the reader's perspective, in order:

- **Badges**, at the top: only badges that are true today (language/.NET-version, preview-features-enabled). No build-status badge (no CI workflow exists yet) and no NuGet badge (not published) - don't add placeholder badges for infrastructure that doesn't exist.
- **WHO/WHAT/WHERE/WHY/HOW headline**: a short, honest description of what Interlocutor is, who it's for, and its current state - this library has not shipped yet, say so plainly rather than implying it's a released package.
- **Brief conceptual grounding**, aimed at a junior developer who may not know these terms, but written so a senior developer doesn't feel talked down to: the Mediator pattern (what Interlocutor implements), how CQRS is commonly layered on top of a mediator, and how Vertical Slice Architecture relates to both. A few sentences each, not an essay - link to `documentation/` for depth if a concept needs more than that.
- **Quick Start**: a small, real, compiling example with value-added inline comments explaining *why* each line is there, not restating what it obviously does. This replaces the current Quick Start section; verify the example still compiles against the shipped API before writing it, don't carry the old one forward unchecked.
- **Library-use table**: one row per package under `src/`, what it does, and when/why a consumer would reach for it. Resolve the same `ValidationFailed` vs `ValidationError` naming discrepancy flagged in ticket 21 - check actual source, don't copy either existing table verbatim.
- **License**: a line stating Apache-2.0 once ticket 20's LICENSE file exists. If this ticket lands first, use a TBD placeholder per house convention (ticket 21) rather than asserting a license that doesn't exist in the repo yet.
- **Index**: links into `documentation/`'s index and the existing `docs/adr/`, `docs/PRD.md` for readers who want the deeper design rationale.

Everything else currently in the README - Composing Pipeline Behaviors, Publishing Notifications, Validation, Authorization, Auditing, Database Transactions, Testing, ASP.NET Core Integration, Interface Segregation, Performance - is removed from the README body and replaced with a link to wherever that content lands in `documentation/` (tickets 23, 28, 30, 31, 32). Where the target doc doesn't exist yet (still in progress or blocked), link with a TBD marker rather than a dead link or, worse, leaving the old content in place.

## Acceptance criteria

- [ ] README is a landing page: badges, headline, concepts, quick start, library table, license, index - no in-depth reference content duplicated from `documentation/`
- [ ] Quick Start example compiles against the shipped API and its comments are value-added, not restating the obvious
- [ ] Library-use table's case-type names are verified against `src/Interlocutor.CommonKernel`, not copied from a stale doc
- [ ] No badge is added for infrastructure that doesn't exist yet (CI, NuGet)
- [ ] License line/badge is either real (if ticket 20 has landed) or a house-convention TBD marker (if not)
- [ ] No content moved to `documentation/` is left duplicated in the README
- [ ] No enumerated counts in hand-written prose (per house convention)

## Blocked by

21 - Establish documentation/ directory; retire CONTEXT.md
