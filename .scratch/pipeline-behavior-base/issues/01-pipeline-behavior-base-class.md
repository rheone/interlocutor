# 01 — PipelineBehaviorBase + DelegatePipelineBehavior

**What to build:** A template-method convenience base class for `IPipelineBehavior<TRequest, TResponse>` implementers, and its delegate-backed sibling, per [../PRD.md](../PRD.md).

Add `PipelineBehaviorBase<TRequest, TResponse>` to `Interlocutor.Abstractions.Pipeline`, implementing `IPipelineBehavior<TRequest, TResponse>.HandleAsync` as a fixed template: call `OnEnterAsync`, skip the inner `next()` call and use the short-circuited response if that hook says to, otherwise await `next()`, then always call `OnExitAsync` with whatever response resulted — a hook that can return a different response than it received. Both hooks default to a no-op (continue / pass-through). `OnExitAsync` runs after a normal completion or an enter-side short-circuit, but never when `next()` throws — the exception propagates untouched, matching `IPipelineStage`'s existing after-hook contract and the shipped `CommitGateDriver`'s actual exception handling.

Add `DelegatePipelineBehavior<TRequest, TResponse>` in the same namespace: a sealed class built from a nullable enter delegate and a nullable exit delegate (either omittable for a no-op), mirroring `DelegatePipelineStage`'s relationship to `IPipelineStage`.

Both types are usable immediately through the *existing* `InterlocutorPipelineBuilder.AddBehavior(Type, RegistrationLifetime)` method — no builder changes in this ticket. Add XML docs on `PipelineBehaviorBase` cross-referencing `IPipelineStage`/`PipelineStageBase` (and vice versa), explaining when to reach for which.

**Blocked by:** None — can start immediately

**Status:** done - implemented in `e195f77`.

- [ ] `PipelineBehaviorBase<TRequest, TResponse>` exists in `Interlocutor.Abstractions.Pipeline`, implements `IPipelineBehavior<TRequest, TResponse>`, and is not sealed/does not expose `HandleAsync` as an extension point (only `OnEnterAsync`/`OnExitAsync` are virtual)
- [ ] Default (no override) behavior is a pure pass-through: `next()` runs, its result is returned unchanged
- [ ] `OnEnterAsync` returning `StageResult<TResponse>.Continue` lets `next()` run and hands its result to `OnExitAsync`
- [ ] `OnEnterAsync` returning a short-circuit skips `next()` entirely but still invokes `OnExitAsync` with the short-circuited response
- [ ] `OnExitAsync` can return a response different from the one it received, and that replacement is what `HandleAsync` returns
- [ ] An exception thrown from `next()` propagates out of `HandleAsync` without `OnExitAsync` running
- [ ] `DelegatePipelineBehavior<TRequest, TResponse>` exists, backed by nullable enter/exit delegates, with either omittable for a no-op default
- [ ] XML docs added on `PipelineBehaviorBase` (and cross-references on `IPipelineStage`/`PipelineStageBase`) explaining the choice between the two extension points
- [ ] `PipelineBehaviorBaseTests.cs` added to `tests/Interlocutor.Abstractions.Tests/` covering: default pass-through, continue-then-exit-sees-result, short-circuit-still-runs-exit, exit-overrides-result, exception-skips-exit
- [ ] `DelegatePipelineBehaviorTests.cs` added to `tests/Interlocutor.Abstractions.Tests/` covering: both delegates supplied, either omitted, both omitted
- [ ] A hand-authored `PipelineBehaviorBase` subclass registers and runs correctly through the existing `InterlocutorPipelineBuilder.AddBehavior(Type, RegistrationLifetime)` path, unchanged
