# 02 — End-to-end proof the generated dispatcher recovers from an eject

**What to build:** `EjectEmissionTests.cs` currently proves only that `DispatchWrapperEmitter`'s output *contains particular substrings* (`Assert.Contains(needle, dispatchSource, ...)`) — it never dispatches a real request through generator-emitted code and observes actual eject recovery. This is the coverage gap that let ticket 01's drift ship unnoticed: the hand-written path has a full end-to-end test (`AuthorizationBehaviorEjectCaseEndToEndTests`), the generated path has none.

After this ticket, a generator test compiles the emitted dispatch source into a real assembly/context, wires a behavior that ejects, and dispatches through the resulting `IGeneratedRequestDispatcher` — mirroring what `AuthorizationBehaviorEjectCaseEndToEndTests` does for the hand-written path. Because this is written after ticket 01 lands, it's proving the single shared `PipelineChainExecutor` call site behaves correctly from the generated path, not standing up a throwaway harness against the duplicated logic being deleted.

**Blocked by:** 01 — Single pipeline-fold implementation shared by both dispatch paths

**Status:** done - implemented in `PLACEHOLDER_SHA`

- [x] A generator test compiles emitted dispatch source into a real assembly/context and dispatches through the resulting `IGeneratedRequestDispatcher`
- [x] The test covers a behavior ejecting via `Eject.With` and one ejecting via `Eject.WithCase`, asserting the final outcome matches what the hand-written path produces for the equivalent scenario
- [x] Substring-only assertions in `EjectEmissionTests` that this new test makes redundant are removed, leaving only what still needs to prove the emitted call site is correctly shaped
