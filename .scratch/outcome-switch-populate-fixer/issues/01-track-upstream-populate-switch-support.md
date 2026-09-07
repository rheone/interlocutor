# 01 - Track upstream "Populate switch" support for `union` types

Status: needs-info

## Labels

needs-info

## What to build

Nothing yet. This ticket exists to hold the trigger for revisiting [ADR-0024](../../../maintainer_docs/adr/0024-defer-union-switch-populate-fixer.md): Interlocutor deliberately did not build a custom analyzer/`CodeFixProvider` to auto-fill missing `switch` arms over `Outcome<...>`/`union` types, because that's a gap in Roslyn's shipped "Populate switch" fixer (`IDE0072` for switch expressions, `IDE0010` for switch statements), not in Interlocutor's own design - the compiler's own exhaustiveness diagnostic (`CS8509`) already names the exact missing case type when switching on a union value directly; the IDE-side auto-fill lightbulb just hasn't caught up to the `union` language feature yet, as of SDK `11.0.100-preview.6.26359.118` (see `global.json`).

When a **released, non-preview** C# toolchain ships `IDE0072`/`IDE0010` support for `union` types (verify hands-on the same way ADR-0024 did: write an incomplete switch over an `Outcome<...>` value, run `dotnet format analyzers --diagnostics IDE0072`, confirm it now offers/fills the missing arms), re-triage this ticket:

- If the upstream fixer now covers the common case well, this can likely just close - no Interlocutor-side work needed.
- If it only covers switching directly on the union value and not on `.Value` (or some other partial gap remains), re-open ADR-0024's "build it ourselves" alternative for the remaining gap, scoped to a new standalone analyzer-only package (not bundled into the opt-in `Interlocutor.Generator`, since this concern is unrelated to compile-time handler dispatch and should reach any consumer).

## Acceptance criteria

- [ ] Re-checked against a released (non-preview) SDK once `union` types ship outside preview
- [ ] `IDE0072`/`IDE0010` behavior against `Outcome<...>` re-verified hands-on (not assumed) the way ADR-0024 verified it
- [ ] Ticket re-triaged to `wontfix (superseded - upstream now covers this)`, or a new ticket opened scoped to whatever gap remains

## Blocked by

Nothing actionable yet - blocked on an external, unscheduled event (a non-preview C# release with `union`-aware `IDE0072`/`IDE0010`), not on any other ticket in this tracker.
