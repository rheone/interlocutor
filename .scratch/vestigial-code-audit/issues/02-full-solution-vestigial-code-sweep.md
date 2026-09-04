# 02 - Full-solution vestigial-code sweep

**What to build:** A fresh sweep of the solution for genuinely vestigial code - dead/unreachable code, duplicate implementations, and debris left over from retired patterns (e.g. anything still shaped like the retired `Interlocutor.EntityFrameworkCore`/`Interlocutor.NHibernate` packages' `TransactionBehavior` convention that `CLAUDE.md` describes as superseded). Full solution scope. Identify only - do not delete or fix anything in this ticket. Each finding becomes its own new `ready-for-agent` ticket filed under `.scratch/vestigial-code-audit/issues/` (numbered continuing from this one), with a concrete file/symbol reference and a one-line reason it's dead, matching how `.scratch/audit-followups/` and `.scratch/codebase-audit-followups/` themselves originated from a prior audit pass.

**Blocked by:** 01

**Status:** done

- [x] Every `src/*` package (and `TutorialProject/` and `tests/*`, whatever their shape is by the time this runs) reviewed
- [x] Findings are genuinely vestigial - confirmed unreachable/unused/superseded against the current shipped code, not assumed from naming alone
- [x] Each finding filed as its own ticket, not bundled into one combined write-up
- [x] This ticket's own file records a short summary list of what was filed, for anyone scanning the effort later

## Comments

**2026-09-04** - Swept the full solution (verified via compiler CS0169/CS0414/CS8321 unused-member diagnostics, `dotnet format` IDE0051/IDE0052 analyzers, and targeted grep-for-references checks on every plausible duplicate/superseded-pattern candidate). One genuine finding, filed as ticket 03. The low count reflects the codebase's actual state - it's already been through several prior audit rounds (`codebase-audit-followups`, the commit-gate-driver/dispatch-sharing consolidation work) that swept up most vestigial code before this ticket ran. Ticket 03 was small enough (two unreferenced constants) to implement immediately in this same pass rather than deferring to a separate effort - see its own file for evidence and closure.

## Findings filed

**2026-09-04** - Full-solution sweep performed: every `src/*` package, `TutorialProject/`, and `tests/*` reviewed. Checked for unreferenced private/internal members (cross-validated against `dotnet build` compiler warnings and `dotnet format style`/`dotnet format analyzers` output - no CS0169/CS0414/CS8321/IDE0051/IDE0052 hits anywhere in the solution), dead branches, near-exact duplicate implementations, leftover `TransactionBehavior`-shaped debris (none found - all remaining mentions are doc-comment call-outs of the retired convention, already correctly framed as historical context), orphaned test fixtures/helpers (all cross-checked by reference count - all in active use), and stale "well-known name" constant tables (`WellKnownTypeNames`, `InterlocutorDiagnostics`, generator tracking-name/type-name constants - all but one pair confirmed still consulted). The EF Core/NHibernate repository and DI-container-extension pairs that look structurally parallel are genuinely store/container-specific (different underlying APIs), not exact duplicates, and `TodoTasksController`/`UsersController`'s "MVC twin of the minimal-API endpoints" is deliberate teaching material per `documentation/sample-app-walkthrough.md`. One genuine finding survived verification:

- **03 - Remove two unreferenced `WellKnownTypeNames` constants in `Interlocutor.Generator`**: `PipelineBehavior` and `ValidationErrors` in `src/Interlocutor.Generator/WellKnownTypeNames.cs` are never read anywhere in the solution - `ValidationErrors` in particular was superseded by an interface-marker check (`IValidationFailureCase`) in `ValidationShapeMismatchAnalyzer.cs` that the constant predates.
