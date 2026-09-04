# Vestigial Code Audit

Status: **not started**. Tracked as issues under `.scratch/vestigial-code-audit/issues/`.

## Original request (paraphrased from a design conversation)

> Seek out and identify vestigial code that could be considered for cleanup, optimization, or deletion.

## Decisions from the grilling session

- **Relationship to the existing audit backlog**: `.scratch/codebase-audit-followups/issues/{08,12,14,18}.md` are already-open, already-ticketed gaps from a prior audit pass (not exactly vestigial code - more "still-unfinished sharing/consolidation work," e.g. a hand-typed reserved-name set that should share a source with the generator's own emitter). Rather than let a fresh sweep rediscover what's already sitting in the tracker, **triage those four first**, then sweep for anything new.
- **Scope of "vestigial"**: genuinely dead/unreachable code, duplicate implementations, and lingering debris from retired patterns (e.g. anything still shaped like the retired `Interlocutor.EntityFrameworkCore`/`Interlocutor.NHibernate` packages' `TransactionBehavior` convention that `CLAUDE.md` describes as superseded). Full solution scope - not narrowed to specific packages.
- **Output**: identify and ticket only. This effort does not delete or fix anything itself - findings become new, individually-filed `ready-for-agent` tickets for a later effort to pick up, matching how `.scratch/audit-followups/` and `.scratch/codebase-audit-followups/` themselves originated from a prior audit pass.

## Ticket checklist

- [ ] 01 - Triage open audit-followup items 08/12/14/18
- [ ] 02 - Full-solution vestigial-code sweep
