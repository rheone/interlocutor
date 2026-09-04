# 01 - Land the ultrareview branch's feature work; retire everything it supersedes

**What to build:** `origin/ultrareview` carries 8 commits, never merged into `main`, that were mid-review when work stopped (last commit: "code-review follow-ups"). They are NOT part of `main` or of the `2026-09-04-CodebaseAuditFollowUps` branch. This ticket is to land that work properly - rebase/merge it onto current `main`, resolve any conflicts with concurrent main-line work (including the `2026-09-04-CodebaseAuditFollowUps` branch's own tickets, several of which touch the same files), and - critically - remove the old implementations each commit's new code was written to replace, so `main` doesn't end up carrying both the old and new version of the same logic side by side.

**The 8 commits, in order, and what each supersedes:**

1. `8b2d7da` - refactor: reorganize `Interlocutor.Generator` into `Models/`/`Analysis/`/`Emission/`/`Diagnostics/` folders (prep, no logic change).
2. `92b95d7` - refactor: extract shared `PipelineChainExecutor` out of `CommandHandlerWrapper`/`VoidCommandHandlerWrapper`/`QueryHandlerWrapper`, each of which currently (on `main`) carries its own ~70-line copy of the same chain-build/eject-guard logic. **Supersedes:** the duplicated chain-build logic inline in all three wrapper classes - once landed, none of the three should still hand-roll it.
3. `b2eb20b` - feat: source-generate `ResultExtensions`'s per-arity `ToActionResult`/`ToMinimalApiResult` overloads via a new `ResultExtensionsGenerator`, replacing the hand-written six-arity blocks in `Interlocutor.AspNetCore/ResultExtensions.cs`. **Supersedes:** the hand-written per-arity blocks in `ResultExtensions.cs` - once landed, that file should contain only the reflective `MapMethodCache`/trimming-documented core, not hand-duplicated arity blocks.
4. `e167acb` - docs: record architecture-review spike decisions (tickets 03, 04) - a `.scratch/` note, not shipped code.
5. `5ee4fd3` - feat: opt-in success/failure classification surface via `: IOutcome` (`.scratch/outcome-union-decoupling/issues/01`, marked done there with full test coverage).
6. `9b4ddef` - feat: per-case-type-named `TryGet{TypeName}` accessors (`.scratch/outcome-union-decoupling/issues/02`, marked done there).
7. `cd21ada` - feat: INTERLOC012 build-time diagnostic for colliding case-accessor names (`.scratch/outcome-union-decoupling/issues/03`, marked done there).
8. `4b63d4f` - fix: code-review follow-ups on the INTERLOC012 rationale.

Commits 5-7 are additive (new opt-in surface, new accessor overloads alongside the existing anonymous ones, a new diagnostic) - per their own ticket files, they don't replace anything, so "retire superseded code" mainly concerns commits 2 and 3 above.

**Blocked by:** None to start the rebase/merge mechanically (git reports no divergence - `main` has no content `ultrareview` lacks, so this is a clean linear replay), but review and file-level conflict resolution against `2026-09-04-CodebaseAuditFollowUps`'s tickets (particularly 04, 09-11, 13, which touch overlapping areas: `Mediator`/wrapper classes, `CommonKernel` result types, persistence behaviors) should happen with both branches' final states in hand - don't start this until that branch's tickets are merged or its fate is otherwise decided.

**Status:** ready-for-agent

- [ ] All 8 commits' functionality is present on `main` after landing (opt-in `: IOutcome` classification, named `TryGet{TypeName}` accessors, INTERLOC012 diagnostic, generated `ResultExtensions`, shared `PipelineChainExecutor`, `Generator` folder structure)
- [ ] `CommandHandlerWrapper`, `VoidCommandHandlerWrapper`, and `QueryHandlerWrapper` no longer each carry their own copy of the chain-build/eject-guard logic - all three call the shared `PipelineChainExecutor`
- [ ] `Interlocutor.AspNetCore/ResultExtensions.cs` no longer hand-writes per-arity `ToActionResult`/`ToMinimalApiResult` blocks - they come from `ResultExtensionsGenerator`
- [ ] No dead code left behind from either superseded implementation
- [ ] Full solution `dotnet build` and `dotnet test` pass after landing
- [ ] `documentation/extensibility.md` reflects the new named-accessor and INTERLOC012 diagnostic surface, per the original tickets' acceptance criteria
