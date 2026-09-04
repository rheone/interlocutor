# 14 - Add a doc-comment map to the Outcome generator's pipeline

> **Originally dropped - false premise (2026-09-04).** This ticket assumed `Interlocutor.Generator` was already split into `Models/`, `Analysis/`, `Emission/`, and `Diagnostics/` subfolders on the `2026-09-04-CodebaseAuditFollowUps` branch. It wasn't - the folder reorg existed only on the local `ultrareview` branch's unpushed commit `8b2d7da`. No action was taken at the time; `ultrareview` was pushed to `origin/ultrareview` for safekeeping, independent of this ticket.

**What to build:** Understanding what `[GenerateOutcomeMembers]` actually produces requires bouncing across seven files (`Models/OutcomeUnionModel.cs`, `Analysis/OutcomeUnionModelBuilder.cs`, `Emission/OutcomeMemberEmitter.cs`, `Analysis/OutcomeAccessorCollisionAnalyzer.cs`, `Diagnostics/InterlocutorDiagnostics.cs`, `OutcomeMemberGenerator.cs`, `CommonKernel/GenerateOutcomeMembersAttribute.cs`). This is documentation-only - the multi-file split itself is correct (Roslyn's incremental-generator caching requires these narrowly-scoped, structurally-equatable snapshot types) and should not be merged. Add an exhaustive doc-comment map to `OutcomeMemberGenerator.cs` (the actual `[Generator]` entry point) naming which file owns which pipeline step, with `<see cref>` links to each, the same way `OutcomeAccessorCollisionAnalyzer`'s remarks already cross-reference `HandlerDiagnosticAnalyzer`.

**Blocked by:** None - can start immediately

**Status:** ready-for-agent

- [ ] `OutcomeMemberGenerator.cs`'s doc comment names all seven files/types in the pipeline and what each owns, with `<see cref>` links
- [ ] No production code changes - this ticket is documentation-only
- [ ] `dotnet build` succeeds with no new doc warnings

## Comments

**2026-09-04** - Revived: `origin/ultrareview` has since been merged into `2026-09-04-ResearchTicketNormalization` (which supersedes `2026-09-04-CodebaseAuditFollowUps`), so `src/Interlocutor.Generator/` is now split into `Models/`/`Analysis/`/`Emission/`/`Diagnostics/`. Confirmed `OutcomeMemberGenerator.cs`'s doc comment does not yet name/link all seven pipeline files. Status reset to `ready-for-agent`.
