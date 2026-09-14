# 01 — Single pipeline-fold implementation shared by both dispatch paths

**What to build:** Today the hand-written dispatch path (`PipelineChainExecutor.ExecuteAsync`) and the generator-emitted dispatch path (`DispatchWrapperEmitter.EmitWrapper`) each run their own copy of the behavior-chain fold and eject-catch logic — one as real C#, the other as hand-written `StringBuilder` text. The generator's copy has already drifted from the hand-written one: commit `fcd5654` added an `Eject.WithCase`/`PipelineEjectCaseException` catch clause to `PipelineChainExecutor` and touched nothing in `DispatchWrapperEmitter`, so a request dispatched through compile-time-generated code that ejects via `Eject.WithCase` currently propagates the exception unhandled instead of recovering.

After this ticket, both dispatch paths execute through the one `PipelineChainExecutor.ExecuteAsync` implementation. A request dispatched through generator-emitted code recovers from `Eject.With`/`Eject.WithCase` exactly the same way a request dispatched through the reflection path does, and any future change to the fold/eject-catch algorithm needs to be made in one file to take effect everywhere.

**Blocked by:** None — can start immediately

**Status:** done - implemented in `3d84a35` (pre-existing prior commit); status line corrected in `51181c5`

- [x] `PipelineChainExecutor.ExecuteAsync` is a public, documented entry point usable by code emitted into a consuming assembly (it already takes only an `IServiceProvider` plus delegates — no other internal surface needs to widen)
- [x] `DispatchWrapperEmitter` emits a call into `PipelineChainExecutor.ExecuteAsync` instead of re-deriving the fold/eject-catch logic as string-templated source
- [x] A request dispatched through generator-emitted code that throws via `Eject.WithCase` recovers correctly (previously this propagated unhandled)
- [x] Existing `EjectEmissionTests`, `AuthorizationBehaviorEjectCaseEndToEndTests`, and the generator's dispatch-switch integration tests all pass
- [x] `.scratch/pipeline-case-fidelity/issues/08-write-adr-eject-withcase.md` is left untouched — this ticket does not write that ADR, it only produces the fix that ADR should weigh as an option
