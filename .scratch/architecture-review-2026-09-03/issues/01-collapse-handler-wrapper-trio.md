# 01 - Extract shared pipeline-chain execution out of the handler-wrapper trio

**What to build:** `CommandHandlerWrapper`, `VoidCommandHandlerWrapper`, and `QueryHandlerWrapper` (`src/Interlocutor.Abstractions/Commands/` and `src/Interlocutor.Abstractions/Queries/`) each carry their own ~70-line copy of the same logic: resolve the handler from DI, tag the activity, build an eject-guarded innermost delegate, then fold `PipelineChainResolver.ResolveChain<...>(services).Reverse()` into it with the eject-guard repeated a second time inside the loop. Extract that shared chain-building/eject logic into one internal executor that all three wrappers call into, resolving only their own handler type first - mirroring how `NotificationHandlerWrapper` (`src/Interlocutor.Abstractions/Notifications/`) already extracted its own shared preamble into a base class. This is a prefactor: no observable behavior change to dispatch, pipeline ordering, or ejection.

**Blocked by:** None - can start immediately.

**Status:** done

- [x] A single internal module owns the chain-build + `PipelineEjectException<TResponse>` guard logic currently duplicated across all three wrapper `Handle` methods
- [x] `CommandHandlerWrapper`, `VoidCommandHandlerWrapper`, and `QueryHandlerWrapper` each shrink to resolving their own handler type and delegating to the shared executor
- [x] Activity tagging behavior is preserved exactly as today for all three request kinds
- [x] Every existing test in `tests/Interlocutor.Abstractions.Tests` (including `MediatorTests`) passes with no modification
- [x] No public API changes - `PublicAPI.Unshipped.txt` for `Interlocutor.Abstractions` is untouched

## Comments

**2026-09-04** - Verified on `2026-09-04-ResearchTicketNormalization` after merging `origin/2026-09-04-CodebaseAuditFollowUps` and `origin/ultrareview`: `PipelineChainExecutor.Execute` is now called from `CommandHandlerWrapper`, `VoidCommandHandlerWrapper`, and `QueryHandlerWrapper` alike; landed via `origin/ultrareview` commit `92b95d7`, merged into this branch. Full solution `dotnet build` and `dotnet test` (1189/1189) pass post-merge.
