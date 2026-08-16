# Interlocutor Documentation

This directory contains reference documentation for developers using Interlocutor. For documentation about building *this repository*, see `docs/` (Architecture Decision Records, PRD, bootstrap reference material).

## Quick navigation

- [Glossary](glossary.md) — domain vocabulary used throughout the project
- **Reference docs** (coming as tickets 23–34 land):
  - Pipeline & Behavior Composition
  - Handler Selection & Dispatch
  - Builder & Registration
  - Mediator, Lifetimes, Scope & Ambient Context
  - Messages, Result Types & Variance
  - Notifications & Fan-out
  - Runtime-Typed Dispatch
  - Transactions & Store Integration
  - ASP.NET Core Integration & HTTP Mapping
  - Testing Handlers & Pipelines
  - Trim & AOT support
  - Sample app walkthrough

- **Design rationale**: [Architecture Decision Records](../docs/adr/)
- **Project PRD**: [docs/PRD.md](../docs/PRD.md)

## House style conventions

Every documentation file in this directory follows these conventions:

### GitHub Alerts

Use all five GitHub alert types purposefully:

- `[!NOTE]` — supplementary detail, in-the-weeds content, or TBD placeholders (always in this form: `> [!NOTE]\n> **TBD:** <what's missing>`, greppable)
- `[!TIP]` — optional but recommended practice
- `[!IMPORTANT]` — load-bearing design decision that's easy to miss
- `[!WARNING]` — common footgun or easy mistake
- `[!CAUTION]` — severe or hard-to-reverse consequence (e.g., a constraint that can never change without a breaking version bump)

### Content rules

- **No enumerated counts in prose**: say "a set of" / "several" and link to source instead. Exception: counts are fine when enumerating an explicit, closed set of type implementations (e.g., "the five `OperationOutcome` failure cases: `NotFound`, `Forbidden`, `Conflict`, `ValidationError`, `Failure`").
- **Auto-generated counts are OK**: badges, CI test counts, etc., since they can't go stale.
- **Diagrams**: inline `​```mermaid` fences only. No standalone `.mmd` files — we don't have reuse-across-docs at scale yet.
- **HTTP status codes**: only `aspnetcore-integration.md` is permitted to reference HTTP status codes. All other docs link there instead.
- **Truthfulness**: every code example compiles against the shipped API. Claims are verified against `src/` code, not carried forward from older docs if they've been superseded.

## TBD sections

Some topics remain TBD until related infrastructure work completes:

- **Trim & AOT support** — blocked on ticket 17 (Trim and AOT hardening)
- **Sample app walkthrough** — blocked on ticket 18 (Sample application)

These sections are written as TBD placeholders per the alert convention above, so nothing is forgotten.
