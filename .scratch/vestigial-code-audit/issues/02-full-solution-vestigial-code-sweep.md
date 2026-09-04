# 02 - Full-solution vestigial-code sweep

**What to build:** A fresh sweep of the solution for genuinely vestigial code - dead/unreachable code, duplicate implementations, and debris left over from retired patterns (e.g. anything still shaped like the retired `Interlocutor.EntityFrameworkCore`/`Interlocutor.NHibernate` packages' `TransactionBehavior` convention that `CLAUDE.md` describes as superseded). Full solution scope. Identify only - do not delete or fix anything in this ticket. Each finding becomes its own new `ready-for-agent` ticket filed under `.scratch/vestigial-code-audit/issues/` (numbered continuing from this one), with a concrete file/symbol reference and a one-line reason it's dead, matching how `.scratch/audit-followups/` and `.scratch/codebase-audit-followups/` themselves originated from a prior audit pass.

**Blocked by:** 01

**Status:** ready-for-agent

- [ ] Every `src/*` package (and `TutorialProject/` and `tests/*`, whatever their shape is by the time this runs) reviewed
- [ ] Findings are genuinely vestigial - confirmed unreachable/unused/superseded against the current shipped code, not assumed from naming alone
- [ ] Each finding filed as its own ticket, not bundled into one combined write-up
- [ ] This ticket's own file records a short summary list of what was filed, for anyone scanning the effort later
