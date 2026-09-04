# 14 - Add a doc-comment map to the Outcome generator's pipeline

**What to build:** Understanding what `[GenerateOutcomeMembers]` actually produces requires bouncing across seven files (`Models/OutcomeUnionModel.cs`, `Analysis/OutcomeUnionModelBuilder.cs`, `Emission/OutcomeMemberEmitter.cs`, `Analysis/OutcomeAccessorCollisionAnalyzer.cs`, `Diagnostics/InterlocutorDiagnostics.cs`, `OutcomeMemberGenerator.cs`, `CommonKernel/GenerateOutcomeMembersAttribute.cs`). This is documentation-only - the multi-file split itself is correct (Roslyn's incremental-generator caching requires these narrowly-scoped, structurally-equatable snapshot types) and should not be merged. Add an exhaustive doc-comment map to `OutcomeMemberGenerator.cs` (the actual `[Generator]` entry point) naming which file owns which pipeline step, with `<see cref>` links to each, the same way `OutcomeAccessorCollisionAnalyzer`'s remarks already cross-reference `HandlerDiagnosticAnalyzer`.

**Blocked by:** None - can start immediately

**Status:** ready-for-agent

- [ ] `OutcomeMemberGenerator.cs`'s doc comment names all seven files/types in the pipeline and what each owns, with `<see cref>` links
- [ ] No production code changes - this ticket is documentation-only
- [ ] `dotnet build` succeeds with no new doc warnings
